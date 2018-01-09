using Elders.Cronus.Pipeline.Config;
using System;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Pipeline.Transport;
using Elders.Cronus.IocContainer;
using Elders.Cronus.Serializer;

namespace Elders.Cronus.Transport.AzureServiceBus.Config
{
    public interface IAzureServiceBusTransportSettings : IPipelineTransportSettings
    {
        string ConnectionString { get; set; }
        TimeSpan LockDuration { get; set; }
        int MaxDeliveryCount { get; set; }
    }


    public class AzureServiceBusTransportSettings : SettingsBuilder, IAzureServiceBusTransportSettings
    {
        public AzureServiceBusTransportSettings(ISettingsBuilder settingsBuilder) : base(settingsBuilder)
        {
            this
                //no default configuration for service bus connection
                .WithEndpointPerBoundedContextNamespace()
                .SetLockDuration(TimeSpan.FromMinutes(1))
                .SetMaxDeliveryCount(5);
        }

        string IAzureServiceBusTransportSettings.ConnectionString { get; set; }
        TimeSpan IAzureServiceBusTransportSettings.LockDuration { get; set; }
        int IAzureServiceBusTransportSettings.MaxDeliveryCount { get; set; }

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

        public static T SetLockDuration<T>(
            this T self,
            TimeSpan lockDuration)
            where T : IAzureServiceBusTransportSettings
        {
            if (lockDuration < TimeSpan.FromSeconds(5) || lockDuration > TimeSpan.FromHours(2))
            {
                throw new Exception("Cannot use lockDuration outside range 5sec..2hr");
            }

            self.LockDuration = lockDuration;

            return self;
        }

        public static T SetMaxDeliveryCount<T>(
            this T self,
            int maxDeliveryCount)
            where T : IAzureServiceBusTransportSettings
        {
            if (maxDeliveryCount < 1 || maxDeliveryCount > 20)
            {
                throw new Exception("Cannot use maxDeliveryCount outside range 1..20");
            }

            self.MaxDeliveryCount = maxDeliveryCount;

            return self;
        }
    }
}
