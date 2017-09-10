using Elders.Cronus.Pipeline.Config;
using System;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Pipeline.Transport;
using Elders.Cronus.IocContainer;
using Elders.Cronus.Serializer;

namespace Cronus.Transport.AzureServiceBus.Config
{
    public interface IAzureServiceBusTransportSettings : IPipelineTransportSettings
    {
        int NumberOfHandlingThreads { get; set; }
        string ConnectionString { get; set; }
    }


    public class AzureServiceBusTransportSettings : SettingsBuilder, IAzureServiceBusTransportSettings
    {
        public AzureServiceBusTransportSettings(ISettingsBuilder settingsBuilder) : base(settingsBuilder)
        {
            this
                //no default configuration for service bus connection
                .WithEndpointPerBoundedContext()
                .SetNumberOfHandlingThreads(1);
        }

        public int NumberOfHandlingThreads { get; set; }
        string IAzureServiceBusTransportSettings.ConnectionString { get; set; }

        IEndpointNameConvention IPipelineTransportSettings.EndpointNameConvention { get; set; }

        IPipelineNameConvention IPipelineTransportSettings.PipelineNameConvention { get; set; }

        public override void Build()
        {
            var builder = this as ISettingsBuilder;
            builder.Container.RegisterSingleton<IPipelineTransport>(() =>
            {
                var serializer = builder.Container.Resolve<ISerializer>();
                return new AzureServiceBusTransport(serializer, this as IAzureServiceBusTransportSettings);
            }, builder.Name);
        }
    }

    public static class AzureServiceBusTransportExtensions
    {
        public static T UseAzureServiceBusTransport<T>(
            this T self,
            Action<IAzureServiceBusTransportSettings> configure = null,
            Action<IPipelineTransportSettings> configureConventions = null)
            where T : ISettingsBuilder
        {
            AzureServiceBusTransportSettings settings = new AzureServiceBusTransportSettings(self as ISettingsBuilder);

            if (configure != null) configure(settings);
            if (configureConventions != null) configureConventions(settings);

            (settings as ISettingsBuilder).Build();

            return self;
        }

        public static T SetNumberOfHandlingThreads<T>(
            this T self,
            int numberOfHandlingThreads)
            where T : IAzureServiceBusTransportSettings
        {
            self.NumberOfHandlingThreads = numberOfHandlingThreads;

            return self;
        }
    }
}