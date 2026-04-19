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

            UserNotificationOnThisDaySettings onThisDay = await db.NotificationOnThisDaySettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            bool hasSubscription = await db.PushSubscriptions
                .AnyAsync(s => s.UserId == userId && s.RevokedAt == null);

            return Ok(CreateModel(settings, newEntries, onThisDay, hasSubscription));
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

            UserNotificationOnThisDaySettings onThisDay = await db.NotificationOnThisDaySettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (onThisDay == null)
            {
                onThisDay = new UserNotificationOnThisDaySettings()
                {
                    UserId = userId
                };
                db.NotificationOnThisDaySettings.Add(onThisDay);
            }

            UserNotificationOnThisDaySettingsModel onThisDayModel = model.OnThisDay ?? new UserNotificationOnThisDaySettingsModel();
            onThisDay.IsEnabled = onThisDayModel.IsEnabled;
            onThisDay.PreferredHour = Math.Clamp(onThisDayModel.PreferredHour, 0, 23);
            onThisDay.TimeZone = NormalizeTimeZone(onThisDayModel.TimeZone);

            await db.SaveChangesAsync();

            bool hasSubscription = await db.PushSubscriptions
                .AnyAsync(s => s.UserId == userId && s.RevokedAt == null);

            return Ok(CreateModel(settings, newEntries, onThisDay, hasSubscription));
        }

        [HttpPost("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SubscribeAsync([FromBody] PushSubscriptionModel model)
        {
            Ensure.NotNull(model, "model");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            string endpoint = model.Endpoint?.Trim();
            string p256dh = model.P256dh?.Trim();
            string auth = model.Auth?.Trim();
            if (String.IsNullOrWhiteSpace(endpoint) || String.IsNullOrWhiteSpace(p256dh) || String.IsNullOrWhiteSpace(auth))
                return BadRequest();

            UserNotificationPushSubscription entity = await db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

            if (entity == null)
            {
                entity = new UserNotificationPushSubscription()
                {
                    CreatedAt = DateTime.Now
                };
                db.PushSubscriptions.Add(entity);
            }
            else if (entity.RevokedAt == null && !String.Equals(entity.UserId, userId, StringComparison.Ordinal))
            {
                return Conflict();
            }

            entity.UserId = userId;
            entity.Endpoint = endpoint;
            entity.P256dh = p256dh;
            entity.Auth = auth;
            entity.LastSeenAt = DateTime.Now;
            entity.RevokedAt = null;

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("subscriptions/{*endpoint}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UnsubscribeAsync([FromRoute] string endpoint)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            endpoint = endpoint?.Trim();
            if (String.IsNullOrWhiteSpace(endpoint))
                return BadRequest();

            UserNotificationPushSubscription entity = await db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

            if (entity != null)
            {
                entity.LastSeenAt = DateTime.Now;
                entity.RevokedAt = DateTime.Now;
                await db.SaveChangesAsync();
            }

            return Ok();
        }
        private UserNotificationSettingsModel CreateModel(UserNotificationSettings settings, UserNotificationNewEntriesSettings newEntries, UserNotificationOnThisDaySettings onThisDay, bool hasSubscription)
        {
            return new UserNotificationSettingsModel()
            {
                IsEnabled = settings?.IsEnabled == true,
                HasSubscription = hasSubscription,
                PushPublicKey = options.PublicKey,
                NewEntries = new UserNotificationNewEntriesSettingsModel()
                {
                    IsEnabled = newEntries?.IsEnabled == true
                },
                OnThisDay = new UserNotificationOnThisDaySettingsModel()
                {
                    IsEnabled = onThisDay?.IsEnabled == true,
                    PreferredHour = onThisDay != null ? Math.Clamp(onThisDay.PreferredHour, 0, 23) : 8,
                    TimeZone = NormalizeTimeZone(onThisDay?.TimeZone)
                }
            };
        }

        private static string NormalizeTimeZone(string timeZone)
        {
            if (String.IsNullOrWhiteSpace(timeZone))
                return "UTC";

            return TimeZoneInfo.TryFindSystemTimeZoneById(timeZone.Trim(), out _)
                ? timeZone.Trim()
                : "UTC";
        }
    }
}
