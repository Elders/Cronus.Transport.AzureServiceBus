using Elders.Cronus.Pipeline;
using System;
using Elders.Cronus.Pipeline.Transport;
using System.Collections.Concurrent;
using Microsoft.ServiceBus;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusTransport : IPipelineTransport, IDisposable
    {
        static ConcurrentDictionary<string, NamespaceManager> namespaceManagers = new ConcurrentDictionary<string, NamespaceManager>();
        string connectionString;

        public AzureServiceBusTransport(Config.IAzureServiceBusTransportSettings settings)
        {
            connectionString = settings.ConnectionString;

            var nmanager = namespaceManagers.GetOrAdd(connectionString, x =>
            {
                var namespaceManager =  NamespaceManager.CreateFromConnectionString(connectionString);
                var testConnection = namespaceManager.GetTopics();
                return namespaceManager;
            });

            PipelineFactory = new AzureServiceBusPipelineFactory(nmanager, settings);
            EndpointFactory = new AzureServiceBusEndpointFactory(nmanager, settings);
        }

        public IEndpointFactory EndpointFactory { get; private set; }

        public IPipelineFactory<IPipeline> PipelineFactory { get; private set; }

        public void Dispose()
        {
            //todo: figure out if anything should happen here
        }
    }
}