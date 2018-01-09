using Elders.Cronus.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.Serializer;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusPipeline : IPipeline, IDisposable
    {
        private readonly string _connectionString;
        private readonly ISerializer _serializer;
        private readonly TimeSpan _lockDuration;
        private readonly int _maxDeliveryCount;

        private TopicClient _client = null;

        public string Name { get; private set; }

        public AzureServiceBusPipeline(
            ISerializer serializer,
            string name,
            Config.IAzureServiceBusTransportSettings settings)
        {
            this._serializer = serializer;
            this.Name = name;
            this._connectionString = settings.ConnectionString;
            this._lockDuration = settings.LockDuration;
            this._maxDeliveryCount = settings.MaxDeliveryCount;
        }

        public void Bind(IEndpoint endpoint)
        {
            var azureEndpoint = (AzureServiceBusEndpoint)endpoint;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(this._connectionString);
            var subscriptionDefinition = new SubscriptionDescription(this.Name, endpoint.Name)
            {
                MaxDeliveryCount = _maxDeliveryCount,
                LockDuration = _lockDuration,
            };
            namespaceManager.TryCreateSubscription(subscriptionDefinition);

            var subscription = namespaceManager.GetSubscription(this.Name, endpoint.Name);
            var subscriptionUpdated = false;

            if (subscriptionDefinition.LockDuration != subscription.LockDuration)
            {
                subscription.LockDuration = subscriptionDefinition.LockDuration;
                subscriptionUpdated = true;
            }

            if (subscriptionDefinition.MaxDeliveryCount != subscription.MaxDeliveryCount)
            {
                subscription.MaxDeliveryCount = subscriptionDefinition.MaxDeliveryCount;
                subscriptionUpdated = true;
            }

            if (subscriptionUpdated)
            {
                namespaceManager.UpdateSubscription(subscription);
            }

            //add filters to subscribe only for needed messages
            var rules = namespaceManager.GetRules(this.Name, endpoint.Name).ToList();
            var rulesHash = new HashSet<string>(rules.Select(x => x.Name));

            var watchMessageTypesHash = new HashSet<string>(azureEndpoint.WatchMessageTypes);
            var client = SubscriptionClient.CreateFromConnectionString(this._connectionString, this.Name, endpoint.Name);

            foreach (var messageType in azureEndpoint.WatchMessageTypes.Where(x => !rulesHash.Contains(x)).ToList())
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

        public void Declare()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(this._connectionString);

            namespaceManager.TryCreateTopic(new TopicDescription(this.Name)
            {
                //RequiresDuplicateDetection = true,
                //DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(10),
            });
        }

        public void Push(CronusMessage message)
        {
            if (_client == null)
            {
                _client = TopicClient.CreateFromConnectionString(this._connectionString, this.Name);
            }

            var body = _serializer.SerializeToBytes(message);

            using (var brokeredMessage = new BrokeredMessage(body))
            {
                var delayInMs = message.GetPublishDelay();
                if (delayInMs > 0)
                {
                    brokeredMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMilliseconds(delayInMs);
                }
                brokeredMessage.MessageId = message.MessageId;
                brokeredMessage.CorrelationId = message.Payload.GetType().GetContractId();
                brokeredMessage.SessionId = message.CorelationId;
                brokeredMessage.ReplyToSessionId = message.CorelationId;
                brokeredMessage.ContentType = "application/octet-stream";
                _client.Send(brokeredMessage);
            }
        }

        public bool Equals(IPipeline other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (_client != null)
            {
                if (_client.IsClosed == false)
                    _client.Close();

                _client = null;
            }
        }
    }
}
