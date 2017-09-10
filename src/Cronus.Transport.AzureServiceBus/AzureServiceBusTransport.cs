using Elders.Cronus.Pipeline;
using Elders.Cronus.Pipeline.Transport;
using Elders.Cronus.Serializer;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusTransport : IPipelineTransport
    {
        public AzureServiceBusTransport(ISerializer serializer, Config.IAzureServiceBusTransportSettings settings)
        {
            PipelineFactory = new AzureServiceBusPipelineFactory(serializer, settings);
            EndpointFactory = new AzureServiceBusEndpointFactory(serializer, settings);
        }

        public IEndpointFactory EndpointFactory { get; private set; }

        public IPipelineFactory<IPipeline> PipelineFactory { get; private set; }
        public void Dispose()
        {
        }
    }
}