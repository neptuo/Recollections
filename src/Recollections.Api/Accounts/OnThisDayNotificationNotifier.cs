using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IServiceScopeFactory scopeFactory;
        private readonly TimeProvider timeProvider;
        private readonly IOptionsMonitor<NotificationOptions> options;
        private readonly ILogger<OnThisDayNotificationNotifier> log;

        public OnThisDayNotificationNotifier(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IOptionsMonitor<NotificationOptions> options,
            ILogger<OnThisDayNotificationNotifier> log)
        {
            Ensure.NotNull(scopeFactory, "scopeFactory");
            Ensure.NotNull(timeProvider, "timeProvider");
            Ensure.NotNull(options, "options");
            Ensure.NotNull(log, "log");
            this.scopeFactory = scopeFactory;
            this.timeProvider = timeProvider;
            this.options = options;
            this.log = log;
        }

        /// <summary>
        /// Current UTC time as seen by the notifier, including the dev-only
        /// <see cref="OnThisDayNotificationOptions.ClockOffset"/>.
        /// </summary>
        public DateTime GetUtcNow()
        {
            DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
            TimeSpan offset = options.CurrentValue?.OnThisDay?.ClockOffset ?? TimeSpan.Zero;
            return offset == TimeSpan.Zero ? utcNow : utcNow + offset;
        }

        /// <summary>
        /// Evaluates every eligible user in isolation and sends at most one
        /// push notification per user per local day. A failure for a single
        /// user is logged and does not abort the tick.
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            using (IServiceScope configScope = scopeFactory.CreateScope())
            {
                if (!configScope.ServiceProvider.GetRequiredService<PushNotificationSender>().IsConfigured)
                {
                    log.LogDebug("Skipping 'on this day' notifications because push delivery is not configured.");
                    return;
                }
            }

            List<UserOnThisDayContext> candidates = await LoadCandidatesAsync(cancellationToken);
            if (candidates.Count == 0)
            {
                log.LogDebug("No eligible users for 'on this day' notifications.");
                return;
            }

            DateTime utcNow = GetUtcNow();
            foreach (UserOnThisDayContext candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessUserAsync(candidate, utcNow, forceSend: false, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "'On this day' notification failed for user '{UserId}'. Continuing with the next user.", candidate.UserId);
                }
            }
        }

        /// <summary>
        /// Runs the notifier for a single user on demand, optionally bypassing
        /// hour gating and dispatch dedupe. Intended for manual end-to-end
        /// validation from the UI / API; no dispatch row is persisted when
        /// <paramref name="forceSend"/> is true so the normal daily schedule
        /// is not affected.
        /// </summary>
        public async Task<OnThisDayTestResult> RunForUserAsync(string userId, bool forceSend, CancellationToken cancellationToken = default)
        {
            Ensure.NotNullOrEmpty(userId, "userId");

            UserOnThisDayContext candidate;
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
                candidate = await accountsDb.NotificationOnThisDaySettings
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .Select(s => new UserOnThisDayContext(s.UserId, s.PreferredHour, s.TimeZone))
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (candidate == null)
                return OnThisDayTestResult.NotConfigured;

            return await ProcessUserAsync(candidate, GetUtcNow(), forceSend, cancellationToken);
        }

        private async Task<OnThisDayTestResult> ProcessUserAsync(UserOnThisDayContext candidate, DateTime utcNow, bool forceSend, CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
            EntriesDataContext entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();
            IConnectionProvider connections = scope.ServiceProvider.GetRequiredService<IConnectionProvider>();
            ShareStatusService shareStatus = scope.ServiceProvider.GetRequiredService<ShareStatusService>();
            PushNotificationSender sender = scope.ServiceProvider.GetRequiredService<PushNotificationSender>();

            TimeZoneInfo timeZone = ResolveTimeZone(candidate.TimeZone);
            DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            DateTime localDate = localNow.Date;

            if (!forceSend && localNow.Hour < candidate.PreferredHour)
            {
                log.LogDebug("Skipping 'on this day' for user '{UserId}': local hour {LocalHour} < preferred {PreferredHour} (tz '{TimeZone}').", candidate.UserId, localNow.Hour, candidate.PreferredHour, timeZone.Id);
                return OnThisDayTestResult.HourNotReached;
            }

            if (!forceSend)
            {
                bool alreadySent = await accountsDb.NotificationOnThisDayDispatches
                    .AsNoTracking()
                    .AnyAsync(d => d.UserId == candidate.UserId && d.Date == localDate, cancellationToken);

                if (alreadySent)
                {
                    log.LogDebug("Skipping 'on this day' for user '{UserId}': already dispatched on {LocalDate:yyyy-MM-dd}.", candidate.UserId, localDate);
                    return OnThisDayTestResult.AlreadySent;
                }
            }

            int matchCount = await CountAnniversaryEntriesAsync(entriesDb, connections, shareStatus, candidate.UserId, localDate, cancellationToken);
            if (matchCount == 0)
            {
                log.LogDebug("Skipping 'on this day' for user '{UserId}': no matching entries on {LocalDate:yyyy-MM-dd}.", candidate.UserId, localDate);
                return OnThisDayTestResult.NoMatches;
            }

            UserNotificationOnThisDayDispatch dispatch = null;
            if (!forceSend)
            {
                dispatch = new UserNotificationOnThisDayDispatch
                {
                    UserId = candidate.UserId,
                    Date = localDate,
                    Created = timeProvider.GetUtcNow().UtcDateTime
                };

                if (!await TryReserveDispatchAsync(accountsDb, dispatch, cancellationToken))
                {
                    log.LogDebug("Skipping 'on this day' for user '{UserId}': dispatch reservation lost a race on {LocalDate:yyyy-MM-dd}.", candidate.UserId, localDate);
                    return OnThisDayTestResult.AlreadySent;
                }
            }

            List<UserNotificationPushSubscription> subscriptions = await accountsDb.PushSubscriptions
                .Where(s => s.RevokedAt == null && s.UserId == candidate.UserId)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                log.LogDebug("Releasing 'on this day' dispatch for user '{UserId}': no active push subscriptions.", candidate.UserId);
                if (dispatch != null)
                {
                    accountsDb.NotificationOnThisDayDispatches.Remove(dispatch);
                    await accountsDb.SaveChangesAsync(cancellationToken);
                }
                return OnThisDayTestResult.NoSubscriptions;
            }

            int delivered;
            try
            {
                delivered = await sender.SendOnThisDayAsync(subscriptions, matchCount, localDate);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to send 'on this day' notification to user '{UserId}' on {LocalDate:yyyy-MM-dd}. Releasing dispatch row.", candidate.UserId, localDate);
                if (dispatch != null)
                {
                    accountsDb.NotificationOnThisDayDispatches.Remove(dispatch);
                    await accountsDb.SaveChangesAsync(cancellationToken);
                }
                return OnThisDayTestResult.DeliveryFailed;
            }

            if (delivered < 1)
            {
                log.LogWarning("'On this day' notification was not delivered to user '{UserId}' on {LocalDate:yyyy-MM-dd}. Releasing dispatch row.", candidate.UserId, localDate);
                if (dispatch != null)
                {
                    accountsDb.NotificationOnThisDayDispatches.Remove(dispatch);
                    await accountsDb.SaveChangesAsync(cancellationToken);
                }
                return OnThisDayTestResult.DeliveryFailed;
            }

            if (dispatch != null)
            {
                dispatch.SentAt = timeProvider.GetUtcNow().UtcDateTime;
                await accountsDb.SaveChangesAsync(cancellationToken);
            }

            log.LogInformation("'On this day' notification delivered to user '{UserId}' for {EntryCount} entries on {LocalDate:yyyy-MM-dd}.", candidate.UserId, matchCount, localDate);
            return OnThisDayTestResult.Sent;
        }

        private async Task<List<UserOnThisDayContext>> LoadCandidatesAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();

            var rows = await accountsDb.NotificationSettings
                .AsNoTracking()
                .Where(s => s.IsEnabled)
                .Join(
                    accountsDb.NotificationOnThisDaySettings.AsNoTracking().Where(s => s.IsEnabled),
                    settings => settings.UserId,
                    topic => topic.UserId,
                    (settings, topic) => new { topic.UserId, topic.PreferredHour, topic.TimeZone }
                )
                .Where(ctx => accountsDb.PushSubscriptions.Any(s => s.RevokedAt == null && s.UserId == ctx.UserId))
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new UserOnThisDayContext(r.UserId, r.PreferredHour, r.TimeZone))
                .ToList();
        }

        private static async Task<int> CountAnniversaryEntriesAsync(
            EntriesDataContext entriesDb,
            IConnectionProvider connections,
            ShareStatusService shareStatus,
            string userId,
            DateTime localDate,
            CancellationToken cancellationToken)
        {
            ConnectedUsersModel connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            IQueryable<Entry> query = shareStatus
                .OwnedByOrExplicitlySharedWithUser(entriesDb, entriesDb.Entries.AsNoTracking(), userId, connectedUsers)
                .Where(e => e.When.Month == localDate.Month && e.When.Day == localDate.Day && e.When.Year != localDate.Year);

            return await query.CountAsync(cancellationToken);
        }

        private static async Task<bool> TryReserveDispatchAsync(AccountsDataContext accountsDb, UserNotificationOnThisDayDispatch dispatch, CancellationToken cancellationToken)
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

    public enum OnThisDayTestResult
    {
        NotConfigured,
        HourNotReached,
        AlreadySent,
        NoMatches,
        NoSubscriptions,
        DeliveryFailed,
        Sent
    }
}
