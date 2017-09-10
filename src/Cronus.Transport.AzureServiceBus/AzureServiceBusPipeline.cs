using Elders.Cronus.Pipeline;
using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusPipeline : IPipeline, IDisposable
    {
        readonly Config.IAzureServiceBusTransportSettings settings;
        readonly NamespaceManager nmanager;
        readonly protected string name;

        private TopicClient _topicClient = null;
        private TopicClient getTopicClient()
        {
            lock (this)
            {
                if(ReferenceEquals(null, _topicClient) == true)
                {
                    _topicClient = TopicClient.CreateFromConnectionString(settings.ConnectionString, name);
                }

                return _topicClient;
            }
        }

        public AzureServiceBusPipeline(string pipelineName, Config.IAzureServiceBusTransportSettings settings, NamespaceManager nmanager)
        {
            this.name = pipelineName;
            this.settings = settings;
            this.nmanager = nmanager;

            nmanager.TryCreateTopic(pipelineName);
        }

        public string Name { get { return name; } }
        public void Bind(IEndpoint endpoint)
        {
            nmanager.TryCreateSubscription(this.name, endpoint.Name);
        }

        public void Push(EndpointMessage message)
        {
            using (var brokeredMessage = new BrokeredMessage(message))
            {
                brokeredMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMilliseconds(message.PublishDelayInMiliseconds);
                getTopicClient().Send(brokeredMessage);
            }
        }

        public bool Equals(IPipeline other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (ReferenceEquals(null, _topicClient) == false)
            {
                this._topicClient.Close();
            }
        }
    }
}