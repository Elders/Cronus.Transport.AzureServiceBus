using Elders.Cronus.Pipeline;
using System.Collections.Generic;
using Elders.Cronus.MessageProcessing;
using Elders.Cronus.Serializer;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusEndpointFactory : IEndpointFactory
    {
        private readonly ISerializer serializer;
        private readonly IEndpointNameConvention endpointNameConvention;
        private readonly Config.IAzureServiceBusTransportSettings settings;

        public AzureServiceBusEndpointFactory(ISerializer serializer, Config.IAzureServiceBusTransportSettings settings)
        {
            this.serializer = serializer;
            this.endpointNameConvention = settings.EndpointNameConvention;
            this.settings = settings;
        }

        public IEndpoint CreateEndpoint(EndpointDefinition definition)
        {
            var pipeline = new AzureServiceBusPipeline(serializer, definition.PipelineName, this.settings);
            pipeline.Declare();

            var endpoint = new AzureServiceBusEndpoint(serializer, definition, settings);
            endpoint.Bind(pipeline);

            return endpoint;
        }

        public IEndpoint CreateTopicEndpoint(EndpointDefinition definition)
        {
            //todo: find out the difference between CreateEndpoint or CreateTopicEndpoint
            return CreateEndpoint(definition);
        }

        public IEnumerable<EndpointDefinition> GetEndpointDefinition(IEndpointConsumer consumer, SubscriptionMiddleware subscriptionMiddleware)
        {
            return endpointNameConvention.GetEndpointDefinition(consumer, subscriptionMiddleware);
        }
    }
}