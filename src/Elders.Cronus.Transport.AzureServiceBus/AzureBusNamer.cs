using System;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public static class AzureBusNamer
    {
        public static string GetTopicName(Type messageType)
        {
            return GetBoundedContext(messageType).ProductNamespace + ".Messages";
        }

        public static string GetSubscriptionName(Type messageType, string name)
        {
            return GetBoundedContext(messageType).ProductNamespace + "." + name;
        }

        static BoundedContextAttribute GetBoundedContext(Type messageType)
        {
            var boundedContext = messageType.GetBoundedContext();

            if (boundedContext == null)
                throw new Exception(String.Format(@"The assembly '{0}' is missing a BoundedContext attribute in AssemblyInfo.cs! Example: [BoundedContext(""Company.Product.BoundedContext"")]", messageType.Assembly.FullName));
            return boundedContext;
        }
    }
}
