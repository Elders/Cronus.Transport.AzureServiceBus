using Elders.Cronus.Pipeline.Config;
using System;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Pipeline.Transport;
using Elders.Cronus.IocContainer;

namespace Cronus.Transport.AzureServiceBus.Config
{
    public interface IAzureServiceBusTransportSettings : IPipelineTransportSettings
    {
        string ConnectionString { get; set; }
    }


    public class AzureServiceBusTransportSettings : SettingsBuilder, IAzureServiceBusTransportSettings
    {
        public AzureServiceBusTransportSettings(ISettingsBuilder settingsBuilder) : base(settingsBuilder)
        {
            //no default configuration for service bus connection
        }

        string IAzureServiceBusTransportSettings.ConnectionString { get; set; }

        IEndpointNameConvention IPipelineTransportSettings.EndpointNameConvention { get; set; }

        IPipelineNameConvention IPipelineTransportSettings.PipelineNameConvention { get; set; }

        public override void Build()
        {
            var builder = this as ISettingsBuilder;
            builder.Container.RegisterSingleton<IPipelineTransport>(() => new AzureServiceBusTransport(this as IAzureServiceBusTransportSettings), builder.Name);
        }
    }

    public static class RabbitMqTransportExtensions
    {
        public static T UseAzureServiceBusTransport<T>(
            this T self,
            Action<IAzureServiceBusTransportSettings> configure = null,
            Action<IPipelineTransportSettings> configureConventions = null)
        {
            AzureServiceBusTransportSettings settings = new AzureServiceBusTransportSettings(self as ISettingsBuilder);
            settings
                //no default configuration for service bus connection
                .WithEndpointPerBoundedContext();

            if (configure != null) configure(settings);
            if (configureConventions != null) configureConventions(settings);

            (settings as ISettingsBuilder).Build();

            return self;
        }
    }
}