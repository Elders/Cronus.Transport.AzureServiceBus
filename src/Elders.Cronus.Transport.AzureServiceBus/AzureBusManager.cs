using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Elders.Cronus.Transport.AzureServiceBus.Logging;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public static class AzureBusExtensions
    {
        public static List<SBTopic> Topics { get; set; }
        public static ConcurrentDictionary<string, List<SBSubscription>> Subscriptions { get; set; }
        public static ConcurrentDictionary<string, List<Rule>> Rules { get; set; }

        public static List<SBTopic> GetTopics(this ServiceBusManagementClient client, string azureBusResourceGroup, string azureBusNamespace)
        {
            if (ReferenceEquals(null, Topics))
                Topics = client.Topics.ListByNamespace(azureBusResourceGroup, azureBusNamespace).ToList();

            return Topics;
        }

        public static List<SBSubscription> GetSubscriptions(this ServiceBusManagementClient client, string azureBusResourceGroup, string azureBusNamespace, string topicName)
        {
            if (Subscriptions == null) Subscriptions = new ConcurrentDictionary<string, List<SBSubscription>>();

            List<SBSubscription> subscr = new List<SBSubscription>();
            if (Subscriptions.TryGetValue(topicName, out subscr) == false)
            {
                var fromAzure = client.Subscriptions.ListByTopic(azureBusResourceGroup, azureBusNamespace, topicName).ToList();
                subscr = fromAzure;
                Subscriptions.AddOrUpdate(topicName, fromAzure, (key, existing) => { existing.AddRange(fromAzure); return existing; });
            }

            return subscr;
        }

        public static List<Rule> GetRules(this ServiceBusManagementClient client, string azureBusResourceGroup, string azureBusNamespace, string topicName, string subscriptionName)
        {
            if (Rules == null) Rules = new ConcurrentDictionary<string, List<Rule>>();

            List<Rule> rules = new List<Rule>();
            if (Rules.TryGetValue(subscriptionName, out rules) == false)
            {
                var fromAzure = client.Rules.ListBySubscriptions(azureBusResourceGroup, azureBusNamespace, topicName, subscriptionName).ToList();
                rules = fromAzure;
                Rules.AddOrUpdate(subscriptionName, fromAzure, (key, existing) => { existing.AddRange(fromAzure); return existing; });
            }

            return rules;
        }
    }

    public class AzureBusManager
    {
        static readonly ILog log = LogProvider.GetLogger(typeof(AzureBusManager));

        static AuthenticationResult tokenResult;
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
                var existingTopic = client.GetTopics(ResourceGroup, serviceBusSettings.Namespace).Where(x => x.Name.Equals(topicName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                var queueParams = new SBTopic()
                {
                    EnablePartitioning = false,
                };

                if (existingTopic == null)
                {
                    var topic = client.Topics.CreateOrUpdate(ResourceGroup, serviceBusSettings.Namespace, topicName, queueParams);
                    AzureBusExtensions.Topics.Add(topic);
                }
            });

            log.Info(() => $"Azure bus topic {topicName}` is ready");
        }

        public void CreateSubscriptionIfNotExists(AzureBusSettings azureBusSettings, string topicName, string subscriptionName, List<string> messageTypes)
        {
            var client = GetServiceBusManagementClient();
            var subscrParams = new SBSubscription()
            {
                AutoDeleteOnIdle = null
            };

            CreateTopicIfNotExists(azureBusSettings, topicName);
            Retryable(() =>
            {
                //mngClient.Subscriptions.Delete(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName);
                var existing = client.GetSubscriptions(ResourceGroup, azureBusSettings.Namespace, topicName).Where(x => x.Name.Equals(subscriptionName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (existing == null)
                {
                    var subscription = client.Subscriptions.CreateOrUpdate(ResourceGroup, azureBusSettings.Namespace, topicName, subscriptionName, subscrParams);
                    if (AzureBusExtensions.Subscriptions.TryGetValue(topicName, out List<SBSubscription> subscriptions))
                        subscriptions.Add(subscription);
                }

            });
            CreateOrUpdateRulesForSubscription(azureBusSettings, topicName, subscriptionName, messageTypes);

            log.Info(() => $"Azure bus subscribtion {subscriptionName} has subscribed to topic {topicName}");
        }

        private void CreateOrUpdateRulesForSubscription(AzureBusSettings serviceBusSettings, string topicName, string subscriptionName, List<string> messageTypes)
        {
            var client = GetServiceBusManagementClient();
            List<string> newRules = new List<string>();
            var existingRules = client.GetRules(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName).ToList();
            foreach (var msgType in messageTypes)
            {
                var ruleName = $@"type-{msgType}".ToLower();
                newRules.Add(ruleName);
                if (existingRules.Any(x => x.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase)) == false)
                {
                    var filter = new Microsoft.Azure.Management.ServiceBus.Models.CorrelationFilter()
                    {
                        CorrelationId = msgType.ToLower()
                    };
                    var rule = new Rule(name: ruleName, filterType: FilterType.CorrelationFilter, correlationFilter: filter);
                    Retryable(() =>
                    {
                        var newRule = client.Rules.CreateOrUpdate(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName, ruleName, rule);
                        if (AzureBusExtensions.Rules.TryGetValue(subscriptionName, out List<Rule> rules))
                            rules.Add(newRule);
                    });
                }
            }
            foreach (var rule in existingRules)
            {
                if (newRules.Contains(rule.Name.ToLower()) == false)
                {
                    Retryable(() =>
                    {
                        client.Rules.Delete(ResourceGroup, serviceBusSettings.Namespace, topicName, subscriptionName, rule.Name);
                        if (AzureBusExtensions.Rules.TryGetValue(subscriptionName, out List<Rule> rules))
                            rules.Remove(rule);

                    });
                }
            }

            Func<List<Rule>, string> rulesAsString = (rules) =>
            {
                StringBuilder flat = new StringBuilder();
                rules.ForEach(r => flat.AppendLine($"{r.Name} {r.CorrelationFilter.CorrelationId} {r.FilterType}"));

                return flat.ToString();
            };

            log.Debug(() =>
            {
                if (AzureBusExtensions.Rules.TryGetValue(subscriptionName, out List<Rule> rulesList))
                    return $"Subscription rule list. Topic:{topicName} Subscription:{subscriptionName}{Environment.NewLine}{rulesAsString(rulesList)}";
                return $"Unable to find rules for subscription {subscriptionName}";
            });
        }

        ServiceBusManagementClient GetServiceBusManagementClient()
        {
            if (managementClient == null || tokenResult == null || DateTimeOffset.UtcNow.AddMinutes(10) > tokenResult.ExpiresOn)
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
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
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
