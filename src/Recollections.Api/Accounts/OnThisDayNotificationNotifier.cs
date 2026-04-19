using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neptuo;
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
    public class OnThisDayNotificationNotifier
    {
        private readonly AccountsDataContext accountsDb;
        private readonly EntriesDataContext entriesDb;
        private readonly IConnectionProvider connections;
        private readonly ShareStatusService shareStatus;
        private readonly PushNotificationSender sender;
        private readonly ILogger<OnThisDayNotificationNotifier> log;

        public OnThisDayNotificationNotifier(AccountsDataContext accountsDb, EntriesDataContext entriesDb, IConnectionProvider connections, ShareStatusService shareStatus, PushNotificationSender sender, ILogger<OnThisDayNotificationNotifier> log)
        {
            Ensure.NotNull(accountsDb, "accountsDb");
            Ensure.NotNull(entriesDb, "entriesDb");
            Ensure.NotNull(connections, "connections");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(sender, "sender");
            Ensure.NotNull(log, "log");
            this.accountsDb = accountsDb;
            this.entriesDb = entriesDb;
            this.connections = connections;
            this.shareStatus = shareStatus;
            this.sender = sender;
            this.log = log;
        }

        public async Task RunAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            if (!sender.IsConfigured)
            {
                log.LogDebug("Skipping 'on this day' notifications because push delivery is not configured.");
                return;
            }

            List<UserOnThisDayContext> candidates = await LoadCandidatesAsync(cancellationToken);
            if (candidates.Count == 0)
            {
                log.LogDebug("No eligible users for 'on this day' notifications.");
                return;
            }

            foreach (UserOnThisDayContext candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TimeZoneInfo timeZone = ResolveTimeZone(candidate.TimeZone);
                DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
                DateTime localDate = localNow.Date;

                if (localNow.Hour < candidate.PreferredHour)
                {
                    log.LogDebug("Skipping 'on this day' for user '{UserId}': local hour {LocalHour} < preferred {PreferredHour} (tz '{TimeZone}').", candidate.UserId, localNow.Hour, candidate.PreferredHour, timeZone.Id);
                    continue;
                }

                bool alreadySent = await accountsDb.NotificationOnThisDayDispatches
                    .AsNoTracking()
                    .AnyAsync(d => d.UserId == candidate.UserId && d.Date == localDate, cancellationToken);

                if (alreadySent)
                {
                    log.LogDebug("Skipping 'on this day' for user '{UserId}': already dispatched on {LocalDate:yyyy-MM-dd}.", candidate.UserId, localDate);
                    continue;
                }

                int matchCount = await CountAnniversaryEntriesAsync(candidate.UserId, localDate, cancellationToken);
                if (matchCount == 0)
                {
                    log.LogDebug("Skipping 'on this day' for user '{UserId}': no matching entries on {LocalDate:yyyy-MM-dd}.", candidate.UserId, localDate);
                    continue;
                }

                UserNotificationOnThisDayDispatch dispatch = new()
                {
                    UserId = candidate.UserId,
                    Date = localDate,
                    Created = DateTime.Now
                };

                if (!await TryReserveDispatchAsync(dispatch, cancellationToken))
                {
                    log.LogDebug("Skipping 'on this day' for user '{UserId}': dispatch reservation lost a race on {LocalDate:yyyy-MM-dd}.", candidate.UserId, localDate);
                    continue;
                }

                List<UserNotificationPushSubscription> subscriptions = await accountsDb.PushSubscriptions
                    .Where(s => s.RevokedAt == null && s.UserId == candidate.UserId)
                    .ToListAsync(cancellationToken);

                if (subscriptions.Count == 0)
                {
                    log.LogDebug("Releasing 'on this day' dispatch for user '{UserId}': no active push subscriptions.", candidate.UserId);
                    accountsDb.NotificationOnThisDayDispatches.Remove(dispatch);
                    await accountsDb.SaveChangesAsync(cancellationToken);
                    continue;
                }

                int delivered = await sender.SendOnThisDayAsync(subscriptions, matchCount, localDate);
                if (delivered < 1)
                {
                    log.LogWarning("'On this day' notification was not delivered to user '{UserId}' on {LocalDate:yyyy-MM-dd}. Releasing dispatch row.", candidate.UserId, localDate);
                    accountsDb.NotificationOnThisDayDispatches.Remove(dispatch);
                }
                else
                {
                    dispatch.SentAt = DateTime.Now;
                    log.LogInformation("'On this day' notification delivered to user '{UserId}' for {EntryCount} entries on {LocalDate:yyyy-MM-dd}.", candidate.UserId, matchCount, localDate);
                }

                if (accountsDb.ChangeTracker.HasChanges())
                    await accountsDb.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<List<UserOnThisDayContext>> LoadCandidatesAsync(CancellationToken cancellationToken)
        {
            return await accountsDb.NotificationSettings
                .AsNoTracking()
                .Where(s => s.IsEnabled)
                .Join(
                    accountsDb.NotificationOnThisDaySettings.AsNoTracking().Where(s => s.IsEnabled),
                    settings => settings.UserId,
                    topic => topic.UserId,
                    (settings, topic) => new UserOnThisDayContext(topic.UserId, topic.PreferredHour, topic.TimeZone)
                )
                .Where(ctx => accountsDb.PushSubscriptions.Any(s => s.RevokedAt == null && s.UserId == ctx.UserId))
                .ToListAsync(cancellationToken);
        }

        private async Task<int> CountAnniversaryEntriesAsync(string userId, DateTime localDate, CancellationToken cancellationToken)
        {
            ConnectedUsersModel connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            IQueryable<Entry> query = shareStatus
                .OwnedByOrExplicitlySharedWithUser(entriesDb, entriesDb.Entries.AsNoTracking(), userId, connectedUsers)
                .Where(e => e.When.Month == localDate.Month && e.When.Day == localDate.Day && e.When.Year != localDate.Year);

            return await query.CountAsync(cancellationToken);
        }

        private async Task<bool> TryReserveDispatchAsync(UserNotificationOnThisDayDispatch dispatch, CancellationToken cancellationToken)
        {
            accountsDb.NotificationOnThisDayDispatches.Add(dispatch);
            try
            {
                await accountsDb.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                accountsDb.Entry(dispatch).State = EntityState.Detached;
                return false;
            }
        }

        private static TimeZoneInfo ResolveTimeZone(string timeZone)
        {
            if (!String.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone.Trim(), out TimeZoneInfo resolved))
                return resolved;

            return TimeZoneInfo.Utc;
        }

        private sealed record UserOnThisDayContext(string UserId, int PreferredHour, string TimeZone);
    }
}
