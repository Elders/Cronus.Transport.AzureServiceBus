using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public class AzureBusManager
    {
        AuthenticationResult tokenResult;
        ServiceBusManagementClient managementClient;

        public AzureBusManager()
        {
            ResourceManagerUrl = "https://management.core.windows.net/";
            ActiveDirectoryAuthority = "https://login.microsoftonline.com/";
        }

        public string ResourceManagerUrl { get; set; }

        public string ActiveDirectoryAuthority { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string SubscriptionId { get; set; }

        public string TenantId { get; set; }

        public string ResourceGroup { get; set; }

        public void CreateTopicIfNotExists(AzureBusSettings serviceBusSettings, string topicName)
        {
            var client = GetServiceBusManagementClient();
            var queueParams = new SBTopic()
            {
                EnablePartitioning = false,
            };
            client.Topics.CreateOrUpdate(ResourceGroup, serviceBusSettings.Namespace, topicName, queueParams);
        }

        public void CreateSubscriptionIfNotExists(AzureBusSettings azureBusSettings, string topicName, string subscriptionName, List<string> messageTypes)
        {
            var mngClient = GetServiceBusManagementClient();
            var subscrParams = new SBSubscription()
            {
                AutoDeleteOnIdle = null
            };

            CreateTopicIfNotExists(azureBusSettings, topicName);

            //mngClient.Subscriptions.Delete(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName);
            mngClient.Subscriptions.CreateOrUpdate(ResourceGroup, azureBusSettings.Namespace, topicName, subscriptionName, subscrParams);
            CreateOrUpdateRulesForSubscription(azureBusSettings, topicName, subscriptionName, messageTypes);
        }

        private void CreateOrUpdateRulesForSubscription(AzureBusSettings serviceBusSettings, string topicName, string subscriptionName, List<string> messageTypes)
        {
            var mngClient = GetServiceBusManagementClient();
            List<string> newRules = new List<string>();
            var existingRules = mngClient.Rules.ListBySubscriptions(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName).ToList();
            foreach (var msgType in messageTypes)
            {
                var ruleName = $@"Type-{msgType}";
                newRules.Add(ruleName);
                if (existingRules.Any(x => x.Name == ruleName) == false)
                {
                    var filter = new Microsoft.Azure.Management.ServiceBus.Models.CorrelationFilter()
                    {
                        CorrelationId = msgType
                    };
                    var rule = new Rule(name: ruleName, filterType: FilterType.CorrelationFilter, correlationFilter: filter);
                    mngClient.Rules.CreateOrUpdate(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName, ruleName, rule);
                }
            }
            foreach (var rule in existingRules)
            {
                if (newRules.Contains(rule.Name) == false)
                    mngClient.Rules.Delete(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName, rule.Name);
            }
        }

        ServiceBusManagementClient GetServiceBusManagementClient()
        {
            if (tokenResult == null || DateTimeOffset.UtcNow.AddMinutes(10) > tokenResult.ExpiresOn)
            {
                var context = new AuthenticationContext($"{ActiveDirectoryAuthority}{TenantId}");

                tokenResult = context.AcquireTokenAsync(
                    ResourceManagerUrl,
                    new ClientCredential(ClientId, ClientSecret)
                ).Result;

                if (tokenResult == null)
                {
                    throw new InvalidOperationException("Failed to obtain the JWT token.");
                }
                var creds = new TokenCredentials(tokenResult.AccessToken);
                managementClient = new ServiceBusManagementClient(creds);
                managementClient.SubscriptionId = SubscriptionId;
            }
            return managementClient;
        }
    }
}
