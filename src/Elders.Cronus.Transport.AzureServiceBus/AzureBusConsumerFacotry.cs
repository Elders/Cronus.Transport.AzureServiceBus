using Elders.Cronus.MessageProcessing;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Serializer;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureBusConsumerFacotry : IConsumerFactory
    {
        private readonly AzureBusSettings serviceBusSettings;
        private readonly AzureBusManager clientSettings;
        private readonly string consumerName;
        private readonly ISerializer serializer;
        private readonly SubscriptionMiddleware middleware;

        public AzureBusConsumerFacotry(AzureBusSettings azureBusSettings, AzureBusManager azureBusManager, string consumerName, ISerializer serializer, SubscriptionMiddleware middleware)
        {
            this.serviceBusSettings = azureBusSettings;
            this.clientSettings = azureBusManager;
            this.consumerName = consumerName;
            this.serializer = serializer;
            this.middleware = middleware;
        }

        public ContinuousConsumer CreateConsumer()
        {
            return new AzureBusContinuousConsumer(serviceBusSettings, clientSettings, consumerName, serializer, middleware);
        }
    }
}
