using Elders.Cronus.Pipeline;
using System;
using Elders.Cronus;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.Serializer;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusPipeline : IPipeline, IDisposable
    {
        private readonly string _connectionString;
        private readonly ISerializer _serializer;
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
        }

        public void Bind(IEndpoint endpoint)
        {
            (endpoint as AzureServiceBusEndpoint)?.Bind(this);
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
            _client?.Close();
        }
    }
}