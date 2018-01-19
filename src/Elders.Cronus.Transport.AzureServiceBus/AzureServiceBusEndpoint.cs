using Elders.Cronus.Pipeline;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.Serializer;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureServiceBusEndpoint : IEndpoint, IDisposable
    {
        private readonly ISerializer _serializer;
        private readonly string _pipelineName;
        private readonly string _connectionString;
        public ICollection<string> WatchMessageTypes { get; private set; }
        private readonly Dictionary<CronusMessage, BrokeredMessage> _dequeuedMessages;
        private SubscriptionClient _client = null;

        public string Name { get; private set; }

        public AzureServiceBusEndpoint(
            ISerializer serializer,
            EndpointDefinition endpointDefinition,
            Config.IAzureServiceBusTransportSettings settings)
        {
            this._serializer = serializer;
            this._pipelineName = endpointDefinition.PipelineName;
            this.Name = endpointDefinition.EndpointName;
            this.WatchMessageTypes = endpointDefinition.WatchMessageTypes.ToList();
            this._connectionString = settings.ConnectionString;
            this._dequeuedMessages = new Dictionary<CronusMessage, BrokeredMessage>();
        }


        public CronusMessage Dequeue(TimeSpan timeout)
        {
            if (_client == null)
            {
                _client = SubscriptionClient.CreateFromConnectionString(this._connectionString, this._pipelineName, this.Name);
            }

            var msg = _client.Receive(timeout);
            if (msg == null)
            {
                return null;
            }

            var transportMessage = (CronusMessage)_serializer.DeserializeFromBytes(msg.GetBody<byte[]>());
            this._dequeuedMessages.Add(transportMessage, msg);

            return transportMessage;
        }

        public void Acknowledge(CronusMessage message)
        {
            try
            {
                BrokeredMessage msg;
                if (_dequeuedMessages.TryGetValue(message, out msg))
                {
                    msg.Complete();
                    this._dequeuedMessages.Remove(message);
                }
            }
            catch (Exception) { Dispose(); throw; }
        }

        public bool Equals(IEndpoint other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (_client != null)
            {
                if (_client.IsClosed == false)
                    _client.Close();

                _client = null;
                this._dequeuedMessages.Clear();
            }
        }
    }
}
