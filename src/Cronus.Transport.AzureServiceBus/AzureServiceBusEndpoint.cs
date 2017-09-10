using Elders.Cronus.Pipeline;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elders.Cronus;
using Elders.Cronus.Serializer;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusEndpoint : IEndpoint, IDisposable
    {
        private readonly ISerializer serializer;
        private readonly string ConnectionString;

        public AzureServiceBusEndpoint(
            ISerializer serializer,
            EndpointDefinition endpointDefinition,
            Config.IAzureServiceBusTransportSettings settings)
        {
            this.serializer = serializer;
            this.Name = endpointDefinition.EndpointName;
            this.WatchMessageTypes = new List<string>(endpointDefinition.WatchMessageTypes);
            this.ConnectionString = settings.ConnectionString;
            this.clients = new List<SubscriptionClient>();
            this.pipelines = new ConcurrentBag<string>();
        }

        private bool Started = false;
        private bool Stopping = false;
        private Action<CronusMessage> onMessage;
        private ConcurrentBag<string> pipelines;
        private List<SubscriptionClient> clients;
        private ConcurrentDictionary<object, object> processedMessages = new ConcurrentDictionary<object, object>();

        public void OnMessage(Action<CronusMessage> action)
        {
            if (Started)
            {
                throw new Exception("Cannot assign 'onMessage' handler when endpoint was started already");
            }

            onMessage = action;
        }

        public void Start()
        {
            if (Started)
            {
                throw new Exception("Cannot start endpoint because it's already started");
            }

            if (onMessage == null)
            {
                throw new Exception("Cannot start endpoint because onMessage handler was not specified yet!");
            }

            Started = true;
            Stopping = false;

            foreach (var pipeline in pipelines)
            {
                var client = SubscriptionClient.CreateFromConnectionString(this.ConnectionString, pipeline, this.Name);

                var options = new OnMessageOptions()
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 2 //for now use only 1 thread per subsciber
                };

                client.OnMessage(msg =>
                {
                    if (client.IsClosed || Stopping)
                    {
                        return;
                    }

                    try
                    {
                        TrackMessage(msg);

                        var cronusMessage = (CronusMessage)serializer.DeserializeFromBytes(msg.GetBody<byte[]>());

                        onMessage(cronusMessage);

                        msg.Complete();
                    }
                    catch (Exception ex)
                    {
                        if (msg.DeliveryCount > 3)
                        {
                            //if retried multiple times & throwed exception, move to dead letter with reason
                            msg.DeadLetter("RuntimeException", $"{ex.GetFullErrorMessage()}\nStack Trace\n{ex.StackTrace}");
                        }
                        else
                        {
                            //msg.Abandon()
                            //problem with msg.Abandon() - it will make this message available right away for reprocessing
                            //which might run in same issue. in order to avoid it, we need to do wait till message lock times out
                        }
                    }
                    finally
                    {
                        LooseMessage(msg);
                    }
                }, options);
            }
        }

        private void TrackMessage(object message)
        {
            processedMessages.TryAdd(message, message);
        }

        private void LooseMessage(object message)
        {
            object result;
            processedMessages.TryRemove(message, out result);
        }

        public void Stop()
        {
            if (!Started || Stopping)
            {
                throw new Exception("Cannot stop endpoint because it was not started!");
            }

            Stopping = true;

            Parallel.ForEach(clients, x => x.Close());

            var timeout = DateTime.UtcNow.AddMinutes(5);
            while (processedMessages.Count > 0 && DateTime.UtcNow < timeout)
            {
                Thread.Sleep(300);
            }

            Started = false;

            clients.Clear();
        }

        public void Bind(IPipeline pipeline)
        {
            pipelines.Add(pipeline.Name);

            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.ConnectionString);
            namespaceManager.TryCreateSubscription(new SubscriptionDescription(pipeline.Name, this.Name)
            {
                LockDuration = TimeSpan.FromSeconds(30), //default is 1 minute
                MaxDeliveryCount = 5, //default is 10
            });

            //add filters to subscribe only for needed messages
            var rules = namespaceManager.GetRules(pipeline.Name, this.Name).ToList();
            var rulesHash = new HashSet<string>(rules.Select(x => x.Name));

            var watchMessageTypesHash = new HashSet<string>(this.WatchMessageTypes);
            var client = SubscriptionClient.CreateFromConnectionString(this.ConnectionString, pipeline.Name, this.Name);

            foreach (var messageType in this.WatchMessageTypes.Where(x => !rulesHash.Contains(x)).ToList())
            {
                client.AddRule(new RuleDescription()
                {
                    Name = messageType,
                    Filter = new CorrelationFilter(messageType)
                });
            }

            var rulesToRemove = rules
                .Where(x => !watchMessageTypesHash.Contains(x.Name))
                .ToList();

            foreach (var ruleToRemove in rulesToRemove)
            {
                client.RemoveRule(ruleToRemove.Name);
            }

            client.Close();
        }

        public string Name { get; private set; }
        public ICollection<string> WatchMessageTypes { get; set; }

        public bool Equals(IEndpoint other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (this.Started)
            {
                this.Stop();
            }
        }
    }
}
