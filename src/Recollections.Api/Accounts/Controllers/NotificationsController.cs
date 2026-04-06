using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Accounts.Notifications;
using System;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Controllers
{
    [ApiController]
    [Route("api/accounts/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly DataContext db;
        private readonly NotificationOptions options;

        public NotificationsController(DataContext db, IOptions<NotificationOptions> options)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(options, "options");
            this.db = db;
            this.options = options.Value;
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(UserNotificationSettingsModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAsync()
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            UserNotificationSettings settings = await db.NotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            UserNotificationNewEntriesSettings newEntries = await db.NotificationNewEntriesSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            bool hasSubscription = await db.PushSubscriptions
                .AnyAsync(s => s.UserId == userId && s.RevokedAt == null);

            return Ok(CreateModel(settings, newEntries, hasSubscription));
        }

        [HttpPut]
        [ProducesDefaultResponseType(typeof(UserNotificationSettingsModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PutAsync([FromBody] UserNotificationSettingsModel model)
        {
            Ensure.NotNull(model, "model");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (model.PreferredHour < 0 || model.PreferredHour > 23)
                return BadRequest();

            UserNotificationSettings settings = await db.NotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new UserNotificationSettings()
                {
                    UserId = userId
                };
                db.NotificationSettings.Add(settings);
            }

            settings.IsEnabled = model.IsEnabled;
            settings.TimeZoneId = String.IsNullOrWhiteSpace(model.TimeZoneId) ? GetDefaultTimeZoneId() : model.TimeZoneId.Trim();
            settings.PreferredHour = NormalizePreferredHour(model.PreferredHour);

            UserNotificationNewEntriesSettings newEntries = await db.NotificationNewEntriesSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (newEntries == null)
            {
                newEntries = new UserNotificationNewEntriesSettings()
                {
                    UserId = userId
                };
                db.NotificationNewEntriesSettings.Add(newEntries);
            }

            newEntries.IsEnabled = model.NewEntries?.IsEnabled == true;

            await db.SaveChangesAsync();

            bool hasSubscription = await db.PushSubscriptions
                .AnyAsync(s => s.UserId == userId && s.RevokedAt == null);

            return Ok(CreateModel(settings, newEntries, hasSubscription));
        }

        [HttpPost("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SubscribeAsync([FromBody] PushSubscriptionModel model)
        {
            Ensure.NotNull(model, "model");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (String.IsNullOrWhiteSpace(model.Endpoint) || String.IsNullOrWhiteSpace(model.P256dh) || String.IsNullOrWhiteSpace(model.Auth))
                return BadRequest();

            UserNotificationPushSubscription entity = await db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == model.Endpoint);

            if (entity == null)
            {
                entity = new UserNotificationPushSubscription()
                {
                    CreatedAt = DateTime.Now
                };
                db.PushSubscriptions.Add(entity);
            }

            entity.UserId = userId;
            entity.Endpoint = model.Endpoint.Trim();
            entity.P256dh = model.P256dh.Trim();
            entity.Auth = model.Auth.Trim();
            entity.LastSeenAt = DateTime.Now;
            entity.RevokedAt = null;

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UnsubscribeAsync([FromBody] PushSubscriptionModel model)
        {
            Ensure.NotNull(model, "model");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (String.IsNullOrWhiteSpace(model.Endpoint))
                return BadRequest();

            UserNotificationPushSubscription entity = await db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == model.Endpoint);

            if (entity != null)
            {
                entity.LastSeenAt = DateTime.Now;
                entity.RevokedAt = DateTime.Now;
                await db.SaveChangesAsync();
            }

            return Ok();
        }

        private UserNotificationSettingsModel CreateModel(UserNotificationSettings settings, UserNotificationNewEntriesSettings newEntries, bool hasSubscription)
        {
            return new UserNotificationSettingsModel()
            {
                IsEnabled = settings?.IsEnabled == true,
                TimeZoneId = settings?.TimeZoneId ?? GetDefaultTimeZoneId(),
                PreferredHour = NormalizePreferredHour(settings?.PreferredHour ?? options.DefaultPreferredHour),
                HasSubscription = hasSubscription,
                PushPublicKey = options.PublicKey,
                NewEntries = new UserNotificationNewEntriesSettingsModel()
                {
                    IsEnabled = newEntries?.IsEnabled == true
                }
            };
        }

        private string GetDefaultTimeZoneId()
            => String.IsNullOrWhiteSpace(options.DefaultTimeZoneId) ? "UTC" : options.DefaultTimeZoneId;

        private static int NormalizePreferredHour(int preferredHour)
            => Math.Max(0, Math.Min(23, preferredHour));
    }
}
