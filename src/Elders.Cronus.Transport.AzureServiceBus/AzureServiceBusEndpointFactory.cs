using Elders.Cronus.Pipeline;
using System.Collections.Generic;
using Elders.Cronus.MessageProcessing;
using Elders.Cronus.Serializer;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusEndpointFactory : IEndpointFactory
    {
        private readonly ISerializer _serializer;
        private readonly IEndpointNameConvention _endpointNameConvention;
        private readonly Config.IAzureServiceBusTransportSettings _settings;

        public AzureServiceBusEndpointFactory(ISerializer serializer, Config.IAzureServiceBusTransportSettings settings)
        {
            this._serializer = serializer;
            this._endpointNameConvention = settings.EndpointNameConvention;
            this._settings = settings;
        }

        public IEndpoint CreateEndpoint(EndpointDefinition definition)
        {
            var pipeline = new AzureServiceBusPipeline(_serializer, definition.PipelineName, this._settings);
            pipeline.Declare();

            var endpoint = new AzureServiceBusEndpoint(_serializer, definition, _settings);
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
            return _endpointNameConvention.GetEndpointDefinition(consumer, subscriptionMiddleware);
        }
    }
}