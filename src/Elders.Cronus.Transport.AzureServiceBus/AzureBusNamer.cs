using System;
using System.Security.Cryptography;
using System.Text;
using Elders.Cronus.Transport.AzureServiceBus.Logging;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public static class AzureBusNamer
    {
        static readonly ILog log = LogProvider.GetLogger(typeof(AzureBusNamer));

        public static string GetTopicName(Type messageType)
        {
            if (typeof(ICommand).IsAssignableFrom(messageType))
                return GetCommandsPipelineName();
            else if (typeof(IEvent).IsAssignableFrom(messageType))
                return GetEventsPipelineName();
            else if (typeof(IScheduledMessage).IsAssignableFrom(messageType))
                return GetEventsPipelineName();
            else
                throw new Exception(string.Format("The message type '{0}' is not eligible. Please use ICommand or IEvent", messageType));
        }

        public static string GetSubscriptionName(Type messageType, string name)
        {
            var bcName = GetBoundedContext(messageType).ProductNamespace;
            return BuildAzureBusResource(bcName, name);
        }

        public static string BuildAzureBusResource(string boundedContext, string name)
        {
            var realName = (boundedContext + "." + name).ToLower();
            var shortName = CalculateMD5Hash(boundedContext) + "." + name.ToLower();

            log.Debug(() => $"Azure bus resource: {realName} : {shortName}");

            return shortName;
        }

        static string GetCommandsPipelineName(Type messageType)
        {
            var bcName = GetBoundedContext(messageType).ProductNamespace;
            return BuildAzureBusResource(bcName, "commands");
        }

        static string GetEventsPipelineName(Type messageType)
        {
            var bcName = GetBoundedContext(messageType).ProductNamespace;
            return BuildAzureBusResource(bcName, "events");
        }

        static string GetEventsPipelineName()
        {
            return BuildAzureBusResource("global", "events");
        }

        static string GetCommandsPipelineName()
        {
            return BuildAzureBusResource("global", "commands");
        }

        public static BoundedContextAttribute GetBoundedContext(Type messageType)
        {
            var boundedContext = messageType.GetBoundedContext();

            if (boundedContext == null)
                throw new Exception(String.Format(@"The assembly '{0}' is missing a BoundedContext attribute in AssemblyInfo.cs! Example: [BoundedContext(""Company.Product.BoundedContext"")]", messageType.Assembly.FullName));
            return boundedContext;
        }

        /// <summary>
        /// https://feedback.azure.com/forums/216926-service-bus/suggestions/18552391-increase-the-maximum-length-of-the-name-of-a-topic
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string CalculateMD5Hash(string input)
        {
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

                byte[] hash = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString().ToLower().Substring(0, 8);
            }
        }
    }
}
