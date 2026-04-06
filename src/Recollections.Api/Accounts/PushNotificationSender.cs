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

        public bool IsConfigured
            => !String.IsNullOrWhiteSpace(options.Subject)
                && !String.IsNullOrWhiteSpace(options.PublicKey)
                && !String.IsNullOrWhiteSpace(options.PrivateKey);

        public Task<int> SendNewEntriesAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, int newEntriesCount)
        {
            Ensure.NotNull(subscriptions, "subscriptions");

            string body = newEntriesCount == 1
                ? "You have 1 new entry waiting in your timeline."
                : $"You have {newEntriesCount} new entries waiting in your timeline.";

            return SendAsync(
                subscriptions,
                new NotificationPayload("New entries from your connections", body, "/", "new-entries")
            );
        }

        private async Task<int> SendAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, NotificationPayload payload)
        {
            if (!IsConfigured)
            {
                log.LogWarning("Push notifications are not configured because the VAPID keys or subject are missing.");
                return 0;
            }

            int deliveredCount = 0;
            string rawPayload = JsonSerializer.Serialize(payload, serializerOptions);
            VapidDetails vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);

            foreach (var subscriptionModel in subscriptions.Where(s => s.RevokedAt == null))
            {
                try
                {
                    PushSubscription subscription = new PushSubscription(
                        subscriptionModel.Endpoint,
                        subscriptionModel.P256dh,
                        subscriptionModel.Auth
                    );

                    await client.SendNotificationAsync(subscription, rawPayload, vapidDetails);
                    deliveredCount++;
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    log.LogInformation(ex, "Revoking stale push subscription '{Endpoint}'.", subscriptionModel.Endpoint);
                    subscriptionModel.RevokedAt = DateTime.Now;
                }
                catch (WebPushException ex)
                {
                    log.LogError(ex, "Failed to deliver push notification to '{Endpoint}'.", subscriptionModel.Endpoint);
                }
            }

            return deliveredCount;
        }

        private record NotificationPayload(string Title, string Body, string Url, string Tag);
    }
}
