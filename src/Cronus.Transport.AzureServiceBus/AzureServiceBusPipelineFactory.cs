using Elders.Cronus.Pipeline;
using System;
using Microsoft.ServiceBus;
using System.Collections.Concurrent;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusPipelineFactory : IPipelineFactory<IPipeline>
    {
        private readonly NamespaceManager namespaceManager;
        private readonly IPipelineNameConvention nameConvention;
        private readonly Config.IAzureServiceBusTransportSettings settings;
        private ConcurrentDictionary<string, IPipeline> pipes = new ConcurrentDictionary<string, IPipeline>();

        public AzureServiceBusPipelineFactory(NamespaceManager nmanager, Config.IAzureServiceBusTransportSettings settings)
        {
            this.namespaceManager = nmanager;
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
                var pipeline = new AzureServiceBusPipeline(pipelineName, settings, namespaceManager);
                return pipeline;
            });
        }
    }
}