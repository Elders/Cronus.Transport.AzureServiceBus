using System;
using Elders.Cronus.Serializer;
using Microsoft.Azure.ServiceBus;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureBusPublisher<TMessage> : Publisher<TMessage>, IDisposable
        where TMessage : IMessage
    {
        private TopicClient topicClient;
        private readonly ISerializer serializer;
        private readonly AzureBusSettings serviceBusSettings;
        private readonly AzureBusManager serviceBusManager;

        public AzureBusPublisher(ISerializer serializer, AzureBusSettings azureBusSettings, AzureBusManager azureBusManager)
        {
            this.serializer = serializer;
            this.serviceBusSettings = azureBusSettings;
            this.serviceBusManager = azureBusManager;
        }

        protected override bool PublishInternal(CronusMessage message)
        {
            if (ReferenceEquals(null, topicClient) || topicClient.IsClosedOrClosing)
            {
                var topicName = AzureBusNamer.GetTopicName(message.Payload.GetType());
                topicClient = new TopicClient(serviceBusSettings.ConnectionString, topicName);
            }

            byte[] body = this.serializer.SerializeToBytes(message);
            var toSend = new Message(body);
            var messageTypeId = message.Payload.GetType().GetContractId().ToLower();
            toSend.CorrelationId = messageTypeId;
            var publishDate = message.GetPublishDate();
            if ((publishDate.UtcDateTime - DateTime.UtcNow).TotalMilliseconds < 1000)
            {
                topicClient.SendAsync(toSend).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                topicClient.ScheduleMessageAsync(toSend, publishDate).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            return true;
        }

        public void Dispose()
        {
            if (ReferenceEquals(null, topicClient) == false || topicClient.IsClosedOrClosing == false)
                topicClient.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
