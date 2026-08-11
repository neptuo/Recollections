using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Accounts.Notifications
{
    /// <summary>
    /// Notifies a user about beings having a birthday on the user's current local day.
    /// </summary>
    public class BirthdayNotificationNotifier : DailyNotificationNotifier<UserNotificationBirthdayDispatch, IReadOnlyCollection<BirthdayNotificationItem>>
    {
        public BirthdayNotificationNotifier(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IOptionsMonitor<NotificationOptions> options,
            ILogger<BirthdayNotificationNotifier> log)
            : base(scopeFactory, timeProvider, options, log)
        { }

        protected override string TopicName => "Birthday";

        protected override TimeSpan GetClockOffset(NotificationOptions options)
            => options?.Birthday?.ClockOffset ?? TimeSpan.Zero;

        protected override DbSet<UserNotificationBirthdayDispatch> GetDispatches(AccountsDataContext db)
            => db.NotificationBirthdayDispatches;

        protected override async Task<List<UserDailyContext>> LoadEnabledUsersAsync(AccountsDataContext db, CancellationToken cancellationToken)
        {
            var rows = await db.NotificationSettings
                .AsNoTracking()
                .Where(s => s.IsEnabled)
                .Join(
                    db.NotificationBirthdaySettings.AsNoTracking().Where(s => s.IsEnabled),
                    settings => settings.UserId,
                    topic => topic.UserId,
                    (settings, topic) => new { topic.UserId, topic.PreferredHour, topic.TimeZone }
                )
                .Where(ctx => db.PushSubscriptions.Any(s => s.RevokedAt == null && s.UserId == ctx.UserId))
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new UserDailyContext(r.UserId, r.PreferredHour, r.TimeZone))
                .ToList();
        }

        protected override Task<UserDailyContext> LoadUserAsync(AccountsDataContext db, string userId, CancellationToken cancellationToken)
            => db.NotificationBirthdaySettings
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new UserDailyContext(s.UserId, s.PreferredHour, s.TimeZone))
                .FirstOrDefaultAsync(cancellationToken);

        protected override async Task<IReadOnlyCollection<BirthdayNotificationItem>> LoadContentAsync(IServiceProvider services, string userId, DateTime localDate, CancellationToken cancellationToken)
        {
            EntriesDataContext entriesDb = services.GetRequiredService<EntriesDataContext>();
            IConnectionProvider connections = services.GetRequiredService<IConnectionProvider>();
            ShareStatusService shareStatus = services.GetRequiredService<ShareStatusService>();

            ConnectedUsersModel connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            IQueryable<Being> accessibleBeings = shareStatus
                .OwnedByOrExplicitlySharedWithUser(entriesDb, entriesDb.Beings.AsNoTracking(), userId, connectedUsers);

            // Load beings with birthday on this date
            List<Being> birthdayBeings = await accessibleBeings
                .Where(b => b.BirthDate != null && b.BirthDate.Value.Month == localDate.Month && b.BirthDate.Value.Day == localDate.Day)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            // Load beings with name day on this date
            List<Being> nameDayBeings = await accessibleBeings
                .Where(b => b.NameDayMonth != null && b.NameDayDay != null && b.NameDayMonth == localDate.Month && b.NameDayDay == localDate.Day)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            var items = new List<BirthdayNotificationItem>();

            // Add birthday notifications
            foreach (var being in birthdayBeings)
            {
                int age = BirthDateUtils.GetAge(being.BirthDate.Value, localDate);
                items.Add(new BirthdayNotificationItem(being.Id, being.Name, age, "birthday"));
            }

            // Add name day notifications, avoiding duplicates if a being has both birthday and name day on the same date
            var birthdayBeingIds = new HashSet<string>(birthdayBeings.Select(b => b.Id));
            foreach (var being in nameDayBeings.Where(b => !birthdayBeingIds.Contains(b.Id)))
            {
                items.Add(new BirthdayNotificationItem(being.Id, being.Name, 0, "nameday"));
            }

            return items;
        }

        protected override bool HasContent(IReadOnlyCollection<BirthdayNotificationItem> beings)
            => beings != null && beings.Count > 0;

        protected override Task<int> SendAsync(PushNotificationSender sender, List<UserNotificationPushSubscription> subscriptions, IReadOnlyCollection<BirthdayNotificationItem> beings, DateTime localDate)
            => sender.SendBirthdayAsync(subscriptions, beings, localDate);
    }

    public record BirthdayNotificationItem(string BeingId, string Name, int Age, string Type);
}
