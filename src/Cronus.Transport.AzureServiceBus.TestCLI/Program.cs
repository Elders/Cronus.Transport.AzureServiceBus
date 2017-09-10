using Cronus.Transport.AzureServiceBus.Config;
using Elders.Cronus.Pipeline.Hosts;
using Elders.Cronus.Pipeline.Transport;

namespace Cronus.Transport.AzureServiceBus.TestCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Elders.Cronus.IocContainer.Container();
            var settings = new CronusSettings(container)
                .UseAzureServiceBusTransport(x =>
                {
                    x.ConnectionString = "Endpoint=sb://z5torm35649294.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=9/hzKz6iWTUskmLyZuQOYT7QmCL9VZ4M+3KLK3elvPw=";
                });

            var transport = container.Resolve(typeof(IPipelineTransport));

        }
    }
}
