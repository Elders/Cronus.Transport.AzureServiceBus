using Elders.Cronus.Pipeline;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Cronus.Transport.AzureServiceBus {
    public class AzureServiceBusEndpointAdapter
    {
        readonly EndpointDefinition endpointDefinition;
        readonly Config.IAzureServiceBusTransportSettings settings;
        readonly NamespaceManager nmanager;

        BlockingCollection<SubscriptionClient> existingClients = new BlockingCollection<SubscriptionClient>();

        public AzureServiceBusEndpointAdapter(EndpointDefinition endpointDefinition, Config.IAzureServiceBusTransportSettings settings, NamespaceManager nmanager)
        {
            this.endpointDefinition = endpointDefinition;
            this.settings = settings;
            this.nmanager = nmanager;
        }

        private bool Initialized = false;
        private void Init()
        {
            lock (this)
            {
                if (Initialized)
                {
                    return;
                }





                Initialized = true;
            }
        }


        public bool BlockDequeue(uint timeoutInMiliseconds, out BrokeredMessage msg)
        {
            Init();

            //var clientToDequeue = existingClients.
            msg = null;
            return false;
        }

        public void Acknowledge(BrokeredMessage message)
        {
            message.Complete();
        }
    }
}
