using System;
using System.Security.Cryptography;
using System.Text;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public static class AzureBusNamer
    {
        public static string GetTopicName(Type messageType)
        {
            var realName = GetBoundedContext(messageType).ProductNamespace + ".Messages";
            var shortName = CalculateMD5Hash(realName); // https://feedback.azure.com/forums/216926-service-bus/suggestions/18552391-increase-the-maximum-length-of-the-name-of-a-topic

            return shortName;
        }

        public static string GetSubscriptionName(Type messageType, string name)
        {
            var realName = GetBoundedContext(messageType).ProductNamespace + "." + name;
            var shortName = CalculateMD5Hash(realName); // https://feedback.azure.com/forums/216926-service-bus/suggestions/18552391-increase-the-maximum-length-of-the-name-of-a-topic

            return shortName;
        }

        static BoundedContextAttribute GetBoundedContext(Type messageType)
        {
            var boundedContext = messageType.GetBoundedContext();

            if (boundedContext == null)
                throw new Exception(String.Format(@"The assembly '{0}' is missing a BoundedContext attribute in AssemblyInfo.cs! Example: [BoundedContext(""Company.Product.BoundedContext"")]", messageType.Assembly.FullName));
            return boundedContext;
        }

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
                return sb.ToString();
            }
        }
    }
}
