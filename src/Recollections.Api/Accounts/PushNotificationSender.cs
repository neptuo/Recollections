using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public class PushNotificationSender
    {
        private readonly NotificationOptions options;
        private readonly WebPushClient client;
        private readonly ILogger<PushNotificationSender> log;
        private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

        public PushNotificationSender(WebPushClient client, IOptions<NotificationOptions> options, ILogger<PushNotificationSender> log)
        {
            Ensure.NotNull(client, "client");
            Ensure.NotNull(options, "options");
            Ensure.NotNull(log, "log");
            this.client = client;
            this.options = options.Value;
            this.log = log;
        }

        public virtual bool IsConfigured
            => !String.IsNullOrWhiteSpace(options.Subject)
                && !String.IsNullOrWhiteSpace(options.PublicKey)
                && !String.IsNullOrWhiteSpace(options.PrivateKey);

        public Task<int> SendNewEntriesAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, IReadOnlyCollection<NewEntryNotificationItem> entries)
        {
            Ensure.NotNull(subscriptions, "subscriptions");
            Ensure.NotNull(entries, "entries");

            if (entries.Count == 1)
            {
                NewEntryNotificationItem entry = entries.First();
                string title = String.IsNullOrWhiteSpace(entry.Title)
                    ? "New entry from your connections"
                    : entry.Title;

                return SendAsync(
                    subscriptions,
                    new NotificationPayload(title, "A new shared entry is waiting for you.", $"/entries/{entry.Id}", "new-entries")
                );
            }

            string body = $"You have {entries.Count} new entries waiting in your timeline.";

            return SendAsync(
                subscriptions,
                new NotificationPayload("New entries from your connections", body, "/", "new-entries")
            );
        }

        public virtual Task<int> SendOnThisDayAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, int entryCount, DateTime localDate)
        {
            Ensure.NotNull(subscriptions, "subscriptions");
            if (entryCount < 1)
                return Task.FromResult(0);

            string body = entryCount == 1
                ? "You have one recollection from this day in a previous year."
                : $"You have {entryCount} recollections from this day in previous years.";
            string tag = $"on-this-day-{localDate:yyyy-MM-dd}";

            return SendAsync(
                subscriptions,
                new NotificationPayload("On this day", body, "/on-this-day", tag)
            );
        }

        private async Task<int> SendAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, NotificationPayload payload)
        {
            bool hasSubject = !String.IsNullOrWhiteSpace(options.Subject);
            bool hasPublicKey = !String.IsNullOrWhiteSpace(options.PublicKey);
            bool hasPrivateKey = !String.IsNullOrWhiteSpace(options.PrivateKey);
            if (!hasSubject || !hasPublicKey || !hasPrivateKey)
            {
                log.LogWarning(
                    "Push notifications are not configured because the VAPID keys or subject are missing. Subject configured: {HasSubject}, public key configured: {HasPublicKey}, private key configured: {HasPrivateKey}.",
                    hasSubject,
                    hasPublicKey,
                    hasPrivateKey
                );
                return 0;
            }

            List<UserNotificationPushSubscription> activeSubscriptions = subscriptions
                .Where(s => s.RevokedAt == null)
                .ToList();

            int deliveredCount = 0;
            string rawPayload = JsonSerializer.Serialize(payload, serializerOptions);
            VapidDetails vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);

            log.LogInformation(
                "Sending push notification '{Tag}' to {SubscriptionCount} active subscription(s).",
                payload.Tag,
                activeSubscriptions.Count
            );

            foreach (var subscriptionModel in activeSubscriptions)
            {
                string endpoint = DescribeEndpoint(subscriptionModel.Endpoint);
                try
                {
                    log.LogDebug(
                        "Delivering push notification '{Tag}' to '{Endpoint}' with payload size {PayloadSize}.",
                        payload.Tag,
                        endpoint,
                        rawPayload.Length
                    );

                    PushSubscription subscription = new PushSubscription(
                        subscriptionModel.Endpoint,
                        subscriptionModel.P256dh,
                        subscriptionModel.Auth
                    );

                    await client.SendNotificationAsync(subscription, rawPayload, vapidDetails);
                    deliveredCount++;
                    log.LogDebug("Push notification '{Tag}' delivered to '{Endpoint}'.", payload.Tag, endpoint);
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    log.LogInformation(ex, "Revoking stale push subscription '{Endpoint}' for notification '{Tag}'.", endpoint, payload.Tag);
                    subscriptionModel.RevokedAt = DateTime.Now;
                }
                catch (WebPushException ex)
                {
                    log.LogError(ex, "Failed to deliver push notification '{Tag}' to '{Endpoint}' with status code '{StatusCode}'. Response body: {ResponseBody}.", payload.Tag, endpoint, ex.StatusCode, ex.Message);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Unexpected error delivering push notification '{Tag}' to '{Endpoint}'.", payload.Tag, endpoint);
                }
            }

            log.LogInformation(
                "Push notification '{Tag}' delivery finished. Delivered to {DeliveredCount} of {SubscriptionCount} active subscription(s).",
                payload.Tag,
                deliveredCount,
                activeSubscriptions.Count
            );

            return deliveredCount;
        }

        private static string DescribeEndpoint(string endpoint)
        {
            if (String.IsNullOrWhiteSpace(endpoint))
                return "<empty>";

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri))
                return $"{uri.Host}{uri.AbsolutePath}";

            return endpoint;
        }

        public record NewEntryNotificationItem(string Id, string Title);

        private record NotificationPayload(string Title, string Body, string Url, string Tag);
    }
}
