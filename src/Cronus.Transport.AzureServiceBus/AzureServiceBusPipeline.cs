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
        private readonly string ConnectionString;
        private readonly ISerializer serializer;
        readonly protected string name;

        private TopicClient client = null;

        public AzureServiceBusPipeline(
            ISerializer serializer,
            string pipelineName,
            Config.IAzureServiceBusTransportSettings settings)
        {
            this.serializer = serializer;
            this.name = pipelineName;
            this.ConnectionString = settings.ConnectionString;
        }

        public void Bind(IEndpoint endpoint)
        {
            (endpoint as AzureServiceBusEndpoint)?.Bind(this);
        }

        public string Name { get { return name; } }

        public void Declare()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.ConnectionString);

            namespaceManager.TryCreateTopic(this.name);
        }

        public void Push(CronusMessage message)
        {
            if (client == null)
            {
                client = TopicClient.CreateFromConnectionString(this.ConnectionString, this.name);
            }

            var body = serializer.SerializeToBytes(message);

            using (var brokeredMessage = new BrokeredMessage(body))
            {
                var delayInMs = message.GetPublishDelay();
                if (delayInMs > 0)
                {
                    brokeredMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMilliseconds(delayInMs);
                }
                brokeredMessage.CorrelationId = message.Payload.GetType().GetContractId();
                brokeredMessage.SessionId = message.CorelationId;
                brokeredMessage.ReplyToSessionId = message.CorelationId;
                brokeredMessage.ContentType = "application/octet-stream";
                client.Send(brokeredMessage);
            }
        }

        public bool Equals(IPipeline other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (ReferenceEquals(null, client) == false)
            {
                client.Close();
            }
        }
    }
}