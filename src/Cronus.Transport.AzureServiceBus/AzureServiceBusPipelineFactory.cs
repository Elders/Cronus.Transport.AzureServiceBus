using Elders.Cronus.Pipeline;
using System;
using System.Collections.Concurrent;
using Elders.Cronus.Serializer;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusPipelineFactory : IPipelineFactory<IPipeline>
    {
        private readonly ISerializer _serializer;
        private readonly IPipelineNameConvention _nameConvention;
        private readonly Config.IAzureServiceBusTransportSettings _settings;
        private ConcurrentDictionary<string, IPipeline> _pipes = new ConcurrentDictionary<string, IPipeline>();

        public AzureServiceBusPipelineFactory(ISerializer serializer, Config.IAzureServiceBusTransportSettings settings)
        {
            this._serializer = serializer;
            this._nameConvention = settings.PipelineNameConvention;
            this._settings = settings;
        }

        public IPipeline GetPipeline(Type messageType)
        {
            string pipelineName = _nameConvention.GetPipelineName(messageType);
            return GetPipeline(pipelineName);
        }

        public IPipeline GetPipeline(string pipelineName)
        {
            return _pipes.GetOrAdd(pipelineName, x =>
            {
                var pipeline = new AzureServiceBusPipeline(_serializer, pipelineName, _settings);
                return pipeline;
            });
        }
    }
}