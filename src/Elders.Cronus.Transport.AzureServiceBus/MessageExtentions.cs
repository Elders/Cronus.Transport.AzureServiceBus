using System;

namespace Elders.Cronus.Transport.AzureServiceBus
{
    public static class MessageExtentions
    {
        public static DateTimeOffset GetPublishDate(this CronusMessage message)
        {
            string publishAt = "0";
            if (message.Headers.TryGetValue(MessageHeader.PublishTimestamp, out publishAt))
            {
                return new DateTimeOffset(DateTime.FromFileTimeUtc(long.Parse(publishAt)));
            }
            return new DateTimeOffset(DateTime.UtcNow);
        }
    }
}
