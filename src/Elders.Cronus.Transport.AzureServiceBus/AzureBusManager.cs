using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            Retryable(() =>
            {
                var existingTopic = client.Topics.ListByNamespace(ResourceGroup, serviceBusSettings.Namespace).ToList().Where(x => x.Name == topicName).FirstOrDefault();
                var queueParams = new SBTopic()
                {
                    EnablePartitioning = false,
                };

                if (existingTopic == null)
                    client.Topics.CreateOrUpdate(ResourceGroup, serviceBusSettings.Namespace, topicName, queueParams);
            });
        }

        public void CreateSubscriptionIfNotExists(AzureBusSettings azureBusSettings, string topicName, string subscriptionName, List<string> messageTypes)
        {
            var mngClient = GetServiceBusManagementClient();
            var subscrParams = new SBSubscription()
            {
                AutoDeleteOnIdle = null
            };

            CreateTopicIfNotExists(azureBusSettings, topicName);
            Retryable(() =>
            {
                //mngClient.Subscriptions.Delete(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName);
                var existing = mngClient.Subscriptions.ListByTopic(ResourceGroup, azureBusSettings.Namespace, topicName).ToList().FirstOrDefault(x => x.Name == subscriptionName);
                if (existing == null)
                    mngClient.Subscriptions.CreateOrUpdate(ResourceGroup, azureBusSettings.Namespace, topicName, subscriptionName, subscrParams);

            });
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
                    Retryable(() =>
                    {
                        var existing = mngClient.Rules.ListBySubscriptions(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName).ToList().Where(x => x.Name == ruleName).FirstOrDefault();
                        if (existing == null)
                            mngClient.Rules.CreateOrUpdate(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName, ruleName, rule);
                    });
                }
            }
            foreach (var rule in existingRules)
            {
                if (newRules.Contains(rule.Name) == false)
                {
                    Retryable(() =>
                    {
                        var existing = mngClient.Rules.ListBySubscriptions(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName).ToList().Where(x => x.Name == rule.Name).FirstOrDefault();
                        if (existing != null)
                            mngClient.Rules.Delete(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName, rule.Name);
                    });
                }
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
                managementClient.SetRetryPolicy(new Microsoft.Rest.TransientFaultHandling.RetryPolicy(new Eveything(), 10, TimeSpan.FromMilliseconds(100)));
            }
            return managementClient;
        }

        void Retryable(System.Action action, int retries = 5, int intervalMiliseconds = 1000)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(intervalMiliseconds);
                }
            }

            throw new Exception("We are really SORRY! We did eveything possible to create an Azure Service bus resource but we were rejected with 429");
        }

        public class Eveything : Microsoft.Rest.TransientFaultHandling.ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                return true;
            }
        }
    }
}
