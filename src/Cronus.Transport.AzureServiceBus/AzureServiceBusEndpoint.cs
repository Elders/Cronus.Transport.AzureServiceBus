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
        private readonly ISerializer _serializer;
        private readonly string _connectionString;
        private readonly int _numberOfHandlingThreads;
        private readonly ICollection<string> _watchMessageTypes;

        public string Name { get; private set; }

        public AzureServiceBusEndpoint(
            ISerializer serializer,
            EndpointDefinition endpointDefinition,
            Config.IAzureServiceBusTransportSettings settings)
        {
            this._serializer = serializer;
            this.Name = endpointDefinition.EndpointName;
            this._watchMessageTypes = new List<string>(endpointDefinition.WatchMessageTypes);
            this._connectionString = settings.ConnectionString;
            this._numberOfHandlingThreads = settings.NumberOfHandlingThreads;
        }

        private bool _started = false;
        private bool _stopping = false;
        private Action<CronusMessage> _onMessageHandler;
        private readonly ConcurrentBag<string> _pipelines = new ConcurrentBag<string>();
        private readonly List<SubscriptionClient> _clients = new List<SubscriptionClient>();
        private readonly ConcurrentDictionary<object, object> _processedMessages = new ConcurrentDictionary<object, object>();

        public void OnMessage(Action<CronusMessage> action)
        {
            if (_started)
            {
                throw new Exception("Cannot assign 'OnMessageHandler' handler when endpoint was started already");
            }

            _onMessageHandler = action;
        }

        public void Start()
        {
            if (_started)
            {
                throw new Exception("Cannot start endpoint because it's already started");
            }

            if (_onMessageHandler == null)
            {
                throw new Exception("Cannot start endpoint because OnMessageHandler handler was not specified yet!");
            }

            _started = true;
            _stopping = false;

            foreach (var pipeline in _pipelines)
            {
                var client = SubscriptionClient.CreateFromConnectionString(this._connectionString, pipeline, this.Name);
                _clients.Add(client);

                var options = new OnMessageOptions()
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = this._numberOfHandlingThreads
                };

                client.OnMessage(msg =>
                {
                    if (client.IsClosed || _stopping)
                    {
                        return;
                    }

                    try
                    {
                        TrackMessage(msg);

                        var cronusMessage = (CronusMessage)_serializer.DeserializeFromBytes(msg.GetBody<byte[]>());

                        _onMessageHandler(cronusMessage);

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
                            //which might run in same issue. in order to avoid it, we need to wait till message lock times out
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
            _processedMessages.TryAdd(message, message);
        }

        private void LooseMessage(object message)
        {
            object result;
            _processedMessages.TryRemove(message, out result);
        }

        public void Stop()
        {
            if (!_started || _stopping)
            {
                throw new Exception("Cannot stop endpoint because it was not started!");
            }

            _stopping = true;

            Parallel.ForEach(_clients, x => x.Close());

            var timeout = DateTime.UtcNow.AddMinutes(5);
            while (_processedMessages.Count > 0 && DateTime.UtcNow < timeout)
            {
                Thread.Sleep(300);
            }

            _started = false;

            _clients.Clear();
        }

        public void Bind(IPipeline pipeline)
        {
            _pipelines.Add(pipeline.Name);

            var namespaceManager = NamespaceManager.CreateFromConnectionString(this._connectionString);
            namespaceManager.TryCreateSubscription(new SubscriptionDescription(pipeline.Name, this.Name)
            {
                LockDuration = TimeSpan.FromSeconds(30), //default is 1 minute
                MaxDeliveryCount = 5, //default is 10
            });

            //add filters to subscribe only for needed messages
            var rules = namespaceManager.GetRules(pipeline.Name, this.Name).ToList();
            var rulesHash = new HashSet<string>(rules.Select(x => x.Name));

            var watchMessageTypesHash = new HashSet<string>(this._watchMessageTypes);
            var client = SubscriptionClient.CreateFromConnectionString(this._connectionString, pipeline.Name, this.Name);

            foreach (var messageType in this._watchMessageTypes.Where(x => !rulesHash.Contains(x)).ToList())
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


        public bool Equals(IEndpoint other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (this._started)
            {
                this.Stop();
            }
        }
    }
}
