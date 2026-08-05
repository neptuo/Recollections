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

        protected override Task<bool> IsAlreadyDispatchedAsync(AccountsDataContext db, string userId, DateTime localDate, CancellationToken cancellationToken)
            => db.NotificationBirthdayDispatches
                .AsNoTracking()
                .AnyAsync(d => d.UserId == userId && d.Date == localDate, cancellationToken);

        protected override async Task<IReadOnlyCollection<BirthdayNotificationItem>> LoadContentAsync(IServiceProvider services, string userId, DateTime localDate, CancellationToken cancellationToken)
        {
            EntriesDataContext entriesDb = services.GetRequiredService<EntriesDataContext>();
            IConnectionProvider connections = services.GetRequiredService<IConnectionProvider>();
            ShareStatusService shareStatus = services.GetRequiredService<ShareStatusService>();

            ConnectedUsersModel connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            List<Being> beings = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(entriesDb, entriesDb.Beings.AsNoTracking(), userId, connectedUsers)
                .Where(b => b.BirthDate != null && b.BirthDate.Value.Month == localDate.Month && b.BirthDate.Value.Day == localDate.Day)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            return beings
                .Select(b => new BirthdayNotificationItem(b.Id, b.Name, BirthDateUtils.GetAge(b.BirthDate.Value, localDate)))
                .ToList();
        }

        protected override bool HasContent(IReadOnlyCollection<BirthdayNotificationItem> beings)
            => beings != null && beings.Count > 0;

        protected override Task<int> SendAsync(PushNotificationSender sender, List<UserNotificationPushSubscription> subscriptions, IReadOnlyCollection<BirthdayNotificationItem> beings, DateTime localDate)
            => sender.SendBirthdayAsync(subscriptions, beings, localDate);
    }

    public record BirthdayNotificationItem(string BeingId, string Name, int Age);
}
