using Elders.Cronus.Pipeline.Config;
using Elders.Cronus.IocContainer;
using Elders.Cronus.Serializer;

namespace Elders.Cronus.Transport.AzureServiceBus.Config
{
    public interface IPipelineTransportSettings : ISettingsBuilder
    {

    }

    public interface IAzureBusTransportSettings : IPipelineTransportSettings
    {
        string ResourceManagerUrl { get; set; }

        string ActiveDirectoryAuthority { get; set; }

        string ClientId { get; set; }

        string ClientSecret { get; set; }

        string SubscriptionId { get; set; }

        string TenantId { get; set; }

        string ResourceGroup { get; set; }

        string Namespace { get; set; }

        string ConnectionString { get; set; }
    }

    public class AzureBusTransportSettings : SettingsBuilder, IAzureBusTransportSettings
    {
        public AzureBusTransportSettings(ISettingsBuilder settingsBuilder) : base(settingsBuilder)
        {
        }

        string IAzureBusTransportSettings.ResourceManagerUrl { get; set; }
        string IAzureBusTransportSettings.ActiveDirectoryAuthority { get; set; }
        string IAzureBusTransportSettings.ClientId { get; set; }
        string IAzureBusTransportSettings.ClientSecret { get; set; }
        string IAzureBusTransportSettings.SubscriptionId { get; set; }
        string IAzureBusTransportSettings.TenantId { get; set; }
        string IAzureBusTransportSettings.ResourceGroup { get; set; }
        string IAzureBusTransportSettings.Namespace { get; set; }
        string IAzureBusTransportSettings.ConnectionString { get; set; }

        public override void Build()
        {
            var builder = this as ISettingsBuilder;
            var config = this as IAzureBusTransportSettings;
            builder.Container.RegisterSingleton<ITransport>(() =>
            {
                var mngClientSettings = new AzureBusManager()
                {
                    ClientId = config.ClientId,
                    ClientSecret = config.ClientSecret,
                    ResourceGroup = config.ResourceGroup,
                    SubscriptionId = config.SubscriptionId,
                    TenantId = config.TenantId
                };

                var serviceBusSettings = new AzureBusSettings()
                {
                    ConnectionString = config.ConnectionString,
                    Namespace = config.Namespace
                };
                var serializer = builder.Container.Resolve<ISerializer>();
                return new AzureBusTransport(serviceBusSettings, mngClientSettings);
            }, builder.Name);
        }
    }
}
