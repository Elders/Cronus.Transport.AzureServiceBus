using System;
using Elders.Cronus.Pipeline.Config;
using Elders.Cronus.Transport.AzureServiceBus.Config;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public static class AzureBusTransportSettingsExtensions
    {
        public static T UseRabbitMqTransport<T>(this T self, Action<IAzureBusTransportSettings> configure = null)
            where T : ISettingsBuilder
        {
            AzureBusTransportSettings settings = new AzureBusTransportSettings(self as ISettingsBuilder);

            if (configure != null) configure(settings);

            (settings as ISettingsBuilder).Build();

            return self;
        }
    }
}
