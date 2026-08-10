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
    public class OnThisDayNotificationNotifier : DailyNotificationNotifier<UserNotificationOnThisDayDispatch, int>
    {
        public OnThisDayNotificationNotifier(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IOptionsMonitor<NotificationOptions> options,
            ILogger<OnThisDayNotificationNotifier> log)
            : base(scopeFactory, timeProvider, options, log)
        { }

        protected override string TopicName => "On this day";

        protected override DbSet<UserNotificationOnThisDayDispatch> GetDispatches(AccountsDataContext db)
            => db.NotificationOnThisDayDispatches;

        protected override async Task<List<UserDailyContext>> LoadEnabledUsersAsync(AccountsDataContext db, CancellationToken cancellationToken)
        {
            var rows = await db.NotificationSettings
                .AsNoTracking()
                .Where(s => s.IsEnabled)
                .Join(
                    db.NotificationOnThisDaySettings.AsNoTracking().Where(s => s.IsEnabled),
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
            => db.NotificationOnThisDaySettings
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new UserDailyContext(s.UserId, s.PreferredHour, s.TimeZone))
                .FirstOrDefaultAsync(cancellationToken);

        protected override async Task<int> LoadContentAsync(IServiceProvider services, string userId, DateTime localDate, CancellationToken cancellationToken)
        {
            EntriesDataContext entriesDb = services.GetRequiredService<EntriesDataContext>();
            IConnectionProvider connections = services.GetRequiredService<IConnectionProvider>();
            ShareStatusService shareStatus = services.GetRequiredService<ShareStatusService>();

            ConnectedUsersModel connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            IQueryable<Entry> query = shareStatus
                .OwnedByOrExplicitlySharedWithUser(entriesDb, entriesDb.Entries.AsNoTracking(), userId, connectedUsers)
                .Where(e => e.When.Month == localDate.Month && e.When.Day == localDate.Day && e.When.Year != localDate.Year);

            return await query.CountAsync(cancellationToken);
        }

        protected override bool HasContent(int entryCount)
            => entryCount > 0;

        protected override Task<int> SendAsync(PushNotificationSender sender, List<UserNotificationPushSubscription> subscriptions, int entryCount, DateTime localDate)
            => sender.SendOnThisDayAsync(subscriptions, entryCount, localDate);
    }
}
