using Elders.Cronus.Pipeline;
using System;
using System.Collections.Generic;
using Elders.Cronus.MessageProcessing;
using Microsoft.ServiceBus;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusEndpointFactory : IEndpointFactory
    {
        private readonly NamespaceManager namespaceManager;
        private readonly IEndpointNameConvention endpointNameConvention;
        private readonly Config.IAzureServiceBusTransportSettings settings;

        public AzureServiceBusEndpointFactory(NamespaceManager nmanager, Config.IAzureServiceBusTransportSettings settings)
        {
            this.namespaceManager = nmanager;
            this.endpointNameConvention = settings.EndpointNameConvention;
            this.settings = settings;
        }

        public IEndpoint CreateEndpoint(EndpointDefinition definition)
        {
            var endpoint = new AzureServiceBusEndpoint(definition, settings, namespaceManager);

            var pipeline = new AzureServiceBusPipeline(definition.PipelineName, this.settings, namespaceManager);
            pipeline.Bind(endpoint);

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