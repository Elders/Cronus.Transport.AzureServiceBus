using Elders.Cronus.Pipeline;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;

namespace Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusEndpoint : IEndpoint, IDisposable
    {
        public AzureServiceBusEndpoint(EndpointDefinition endpointDefinition, Config.IAzureServiceBusTransportSettings settings, NamespaceManager nmanager)
        {
            Name = endpointDefinition.EndpointName;
            RoutingKey = endpointDefinition.RoutingKey;
            RoutingHeaders = new Dictionary<string, object>(endpointDefinition.RoutingHeaders);
            Adapter = new AzureServiceBusEndpointAdapter(endpointDefinition, settings, nmanager);

            dequeuedMessages = new Dictionary<EndpointMessage, BrokeredMessage>();
        }

        private Dictionary<EndpointMessage, BrokeredMessage> dequeuedMessages;

        private AzureServiceBusEndpointAdapter Adapter { get; set; }

        public string Name { get; private set; }

        public IDictionary<string, object> RoutingHeaders { get; set; }

        public string RoutingKey { get; set; }

        public void Acknowledge(EndpointMessage message)
        {
            Adapter.Acknowledge(dequeuedMessages[message]);
        }

        public void AcknowledgeAll()
        {
            foreach (var msg in dequeuedMessages)
            {
                Acknowledge(msg.Key);
            }

            dequeuedMessages.Clear();
        }

        public bool BlockDequeue(uint timeoutInMiliseconds, out EndpointMessage msg)
        {
            msg = null;
            BrokeredMessage mg;
            if (Adapter.BlockDequeue(timeoutInMiliseconds, out mg) == true)
            {
                msg = mg.GetBody<EndpointMessage>();
            }

            if (mg != null)
            {
                dequeuedMessages[msg] = mg;
            }

            return msg != null;
        }


        public EndpointMessage DequeueNoWait()
        {
            EndpointMessage result;
            if (!BlockDequeue(0, out result))
            {
                return null;
            }
            return result;
        }

        public bool Equals(IEndpoint other)
        {
            throw new NotImplementedException();
        }

        public void Open()
        {
        }
        public void Close()
        {
        }
        public void Dispose()
        {
        }
    }
}
