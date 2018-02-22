using System;
using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.MessageProcessing;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Serializer;
using Elders.Cronus.Transport.AzureServiceBus.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureBusContinuousConsumer : ContinuousConsumer
    {
        static readonly ILog log = LogProvider.GetLogger(typeof(AzureBusContinuousConsumer));

        private readonly ISerializer serializer;

        private Dictionary<Guid, Message> deliveryTags;

        private SubscriptionClient correlationFilterSubscriptionClient;
        private IMessageReceiver subscriptionReceiver;

        public AzureBusContinuousConsumer(AzureBusSettings azureBusSettings, AzureBusManager azureBusManager, string consumerName, ISerializer serializer, SubscriptionMiddleware middleware)
            : base(middleware)
        {
            this.deliveryTags = new Dictionary<Guid, Message>();
            this.serializer = serializer;
            var messageTypes = middleware.Subscribers.SelectMany(x => x.GetInvolvedMessageTypes()).Distinct().ToList();
            var topicName = AzureBusNamer.GetTopicName(messageTypes.FirstOrDefault());
            var subscriptionName = AzureBusNamer.GetSubscriptionName(messageTypes.FirstOrDefault(), consumerName);
            azureBusManager.CreateSubscriptionIfNotExists(azureBusSettings, topicName, subscriptionName, messageTypes.Select(x => x.GetContractId()).ToList());
            correlationFilterSubscriptionClient = new SubscriptionClient(azureBusSettings.ConnectionString, topicName, subscriptionName);
            string subscriptionPath = EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName);
            subscriptionReceiver = new MessageReceiver(azureBusSettings.ConnectionString, subscriptionPath, ReceiveMode.PeekLock);
        }

        protected override CronusMessage GetMessage()
        {
            var dequeuedMessage = subscriptionReceiver.ReceiveAsync(TimeSpan.FromSeconds(30)).Result;

            if (ReferenceEquals(null, dequeuedMessage) == false)
            {
                var cronusMessage = (CronusMessage)serializer.DeserializeFromBytes(dequeuedMessage.Body);
                deliveryTags[cronusMessage.Id] = dequeuedMessage;
                return cronusMessage;
            }
            return null;
        }

        protected override void MessageConsumed(CronusMessage message)
        {
            try
            {
                Message busMessage;
                if (deliveryTags.TryGetValue(message.Id, out busMessage))
                    subscriptionReceiver.CompleteAsync(busMessage.SystemProperties.LockToken).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            finally
            {
                deliveryTags.Remove(message.Id);
            }

        }

        protected override void WorkStart() { }

        protected override void WorkStop()
        {
            subscriptionReceiver.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            deliveryTags?.Clear();
            deliveryTags = null;
        }
    }
}
