﻿using System;
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
            if (topicClient == null)
            {
                var topicName = AzureBusNamer.GetTopicName(message.Payload.GetType());
                serviceBusManager.CreateTopicIfNotExists(serviceBusSettings, topicName);
                topicClient = new TopicClient(serviceBusSettings.ConnectionString, topicName);
            }

            byte[] body = this.serializer.SerializeToBytes(message);
            var toSend = new Message(body);
            var messageTypeId = message.Payload.GetType().GetContractId();
            toSend.CorrelationId = messageTypeId;
            var publishDate = message.GetPublishDate();
            if ((publishDate.UtcDateTime - DateTime.UtcNow).TotalMilliseconds < 1000)
            {
                topicClient.SendAsync(toSend).GetAwaiter().GetResult();
            }
            else
            {
                topicClient.ScheduleMessageAsync(toSend, publishDate).GetAwaiter().GetResult();
            }
            return true;
        }

        public void Dispose()
        {
            topicClient.CloseAsync().GetAwaiter().GetResult();
        }
    }
}