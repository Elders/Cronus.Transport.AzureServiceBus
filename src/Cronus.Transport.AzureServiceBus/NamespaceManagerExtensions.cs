using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics;
using System.Threading;

namespace Cronus.Transport.AzureServiceBus
{
    public static class NamespaceManagerExtensions
    {
        public static void TryCreateTopic(this NamespaceManager self, TopicDescription topic)
        {
            self.TryCreateTopic(topic, 3, TimeSpan.FromSeconds(3));
        }

        public static void TryCreateTopic(this NamespaceManager self, TopicDescription topic, int retries, TimeSpan retryDelay)
        {
            var createTopicAction = new Action(() =>
            {
                if (self.TopicExists(topic.Path))
                {
                    return;
                }

                try
                {
                    self.CreateTopic(topic);
                    return;
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // it is possible that there is a distributed race condition between publisher
                    // and subscriber where both may attempt to create a topic that doesn't exist
                    // and only the first one will succeed.  the second one will fail with an exception
                    // that the topic already exists.  in that case, it is not bad to ignore
                    // the very specific exception that the message entity already exists because the
                    // end result is the same for all cases....a new topic will be successfully created.
                    Debug.WriteLine("The topic '{0}' already exists.", topic);
                    return;
                }
            });

            // try to create the topic, but wrap in a retry block because it is possible that two
            // separate distributed processes might try to create the topic at the same time but neither
            // has fully succeeded yet.  in which case, a MessagingException will be thrown for conflicting
            // operations.  this case is slightly different than the one noted in the catch block above in that
            // the catch block above will happen because another process already fully succeeded in creating
            // the topic before this process finished.  this case handles the situation where multiple instances
            // have all started the process of topic creation but none have fully succeeded.  in that case, only
            // one will be allowed to continue and the rest will receive a MessagingException for conflicting
            // operation.  the attempt is wrapped in a retry so that if none fully succeed, the activity will
            // be tried again.  if one does fully succeed, the retry will simply see that the topic has already
            // been created and exit.

            createTopicAction.InvokeAndRetryOnException<MessagingException>(retries, retryDelay);
        }

        public static void TryCreateSubscription(this NamespaceManager self, SubscriptionDescription subscription)
        {
            self.TryCreateSubscription(subscription, 3, TimeSpan.FromSeconds(3));
        }

        public static void TryCreateSubscription(this NamespaceManager self, SubscriptionDescription subscription, int retries, TimeSpan retryDelay)
        {
            var createSubscriptionAction = new Action(() =>
            {
                if (self.SubscriptionExists(subscription.TopicPath, subscription.Name))
                {
                    return;
                }

                try
                {
                    self.CreateSubscription(subscription);
                    return;
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Debug.WriteLine("The subscription '{1}' for topic '{0}' already exists.", subscription.TopicPath, subscription.Name);
                    return;
                }
            });

            createSubscriptionAction.InvokeAndRetryOnException<MessagingException>(retries, retryDelay);
        }
    }

    public static class ActionExtentions
    {
        public static void InvokeAndRetryOnException<T>(this Action action, int retries, TimeSpan retryDelay)
            where T : Exception
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            while (retries-- > 0)
            {
                try
                {
                    action();
                    return;
                }
                catch (T)
                {
                    Thread.Sleep(retryDelay);
                    Debug.WriteLine("Caught exception '{0}'.  Retrying '{1}'", typeof(T), action);
                }
            }

            action();
        }
    }
}
