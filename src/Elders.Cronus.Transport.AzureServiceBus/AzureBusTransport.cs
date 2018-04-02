using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.MessageProcessing;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Serializer;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureBusTransport : ITransport
    {
        private readonly AzureBusSettings serviceBusSettings;
        private readonly AzureBusManager clientSettings;

        public AzureBusTransport(AzureBusSettings azureBusSettings, AzureBusManager azureBusManager)
        {
            this.serviceBusSettings = azureBusSettings;
            this.clientSettings = azureBusManager;
        }
        public IEnumerable<IConsumerFactory> GetAvailableConsumers(ISerializer serializer, SubscriptionMiddleware subscriptions, string consumerName)
        {
            //foreach (var subscriber in subscriptions.Subscribers)
            {
                var messageType = subscriptions.Subscribers.FirstOrDefault().GetInvolvedMessageTypes().FirstOrDefault();
                var bc = AzureBusNamer.GetBoundedContext(messageType).ProductNamespace;
                var name = AzureBusNamer.BuildAzureBusResource(bc, consumerName);

                yield return new AzureBusConsumerFacotry(serviceBusSettings, clientSettings, name, serializer, subscriptions);
            }
        }

        public IPublisher<TMessage> GetPublisher<TMessage>(ISerializer serializer) where TMessage : IMessage
        {
            return new AzureBusPublisher<TMessage>(serializer, serviceBusSettings, clientSettings);
        }
    }
}
