using Elders.Cronus.Pipeline;
using System;
using System.Collections.Concurrent;
using Elders.Cronus.Serializer;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusPipelineFactory : IPipelineFactory<IPipeline>
    {
        private readonly ISerializer serializer;
        private readonly IPipelineNameConvention nameConvention;
        private readonly Config.IAzureServiceBusTransportSettings settings;
        private ConcurrentDictionary<string, IPipeline> pipes = new ConcurrentDictionary<string, IPipeline>();

        public AzureServiceBusPipelineFactory(ISerializer serializer, Config.IAzureServiceBusTransportSettings settings)
        {
            this.serializer = serializer;
            this.nameConvention = settings.PipelineNameConvention;
            this.settings = settings;
        }

        public IPipeline GetPipeline(Type messageType)
        {
            string pipelineName = nameConvention.GetPipelineName(messageType);
            return GetPipeline(pipelineName);
        }

        public IPipeline GetPipeline(string pipelineName)
        {
            return pipes.GetOrAdd(pipelineName, x =>
            {
                var pipeline = new AzureServiceBusPipeline(serializer, pipelineName, settings);
                return pipeline;
            });
        }
    }
}