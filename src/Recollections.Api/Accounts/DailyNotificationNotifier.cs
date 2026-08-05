using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public enum DailyNotificationResult
    {
        NotConfigured,
        HourNotReached,
        AlreadySent,
        NoMatches,
        NoSubscriptions,
        DeliveryFailed,
        Sent
    }

    /// <summary>
    /// Shared orchestration for notification topics that are evaluated once per user's
    /// local day: hour gating in the user's time zone, per-day dedupe using a dispatch
    /// row, push delivery and dispatch release when delivery fails.
    /// </summary>
    public abstract class DailyNotificationNotifier<TDispatch, TContent>
        where TDispatch : class, IUserNotificationDailyDispatch, new()
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly TimeProvider timeProvider;
        private readonly IOptionsMonitor<NotificationOptions> options;
        private readonly ILogger log;

        protected DailyNotificationNotifier(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IOptionsMonitor<NotificationOptions> options,
            ILogger log)
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
        /// Human readable topic name used in log messages.
        /// </summary>
        protected abstract string TopicName { get; }

        protected abstract DbSet<TDispatch> GetDispatches(AccountsDataContext db);

        /// <summary>
        /// Loads users that have both the master switch and this topic enabled.
        /// </summary>
        protected abstract Task<List<UserDailyContext>> LoadEnabledUsersAsync(AccountsDataContext db, CancellationToken cancellationToken);

        /// <summary>
        /// Loads this topic's settings of a single user or <c>null</c> when not configured.
        /// </summary>
        protected abstract Task<UserDailyContext> LoadUserAsync(AccountsDataContext db, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns whether this topic was already dispatched to the user on the local day.
        /// </summary>
        protected abstract Task<bool> IsAlreadyDispatchedAsync(AccountsDataContext db, string userId, DateTime localDate, CancellationToken cancellationToken);

        /// <summary>
        /// Loads the content to notify about. Return content for which
        /// <see cref="HasContent(TContent)"/> is <c>false</c> when there is nothing to send.
        /// </summary>
        protected abstract Task<TContent> LoadContentAsync(IServiceProvider services, string userId, DateTime localDate, CancellationToken cancellationToken);

        protected abstract bool HasContent(TContent content);

        protected abstract Task<int> SendAsync(PushNotificationSender sender, List<UserNotificationPushSubscription> subscriptions, TContent content, DateTime localDate);

        protected virtual TimeSpan GetClockOffset(NotificationOptions options)
            => options?.OnThisDay?.ClockOffset ?? TimeSpan.Zero;

        /// <summary>
        /// Current UTC time as seen by the notifier, including the dev-only clock offset.
        /// </summary>
        public DateTime GetUtcNow()
        {
            DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
            TimeSpan offset = GetClockOffset(options.CurrentValue);
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
                    log.LogDebug("Skipping '{Topic}' notifications because push delivery is not configured.", TopicName);
                    return;
                }
            }

            List<UserDailyContext> candidates = await LoadCandidatesAsync(cancellationToken);
            if (candidates.Count == 0)
            {
                log.LogDebug("No eligible users for '{Topic}' notifications.", TopicName);
                return;
            }

            DateTime utcNow = GetUtcNow();
            foreach (UserDailyContext candidate in candidates)
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
                    log.LogError(ex, "'{Topic}' notification failed for user '{UserId}'. Continuing with the next user.", TopicName, candidate.UserId);
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
        public async Task<DailyNotificationResult> RunForUserAsync(string userId, bool forceSend, CancellationToken cancellationToken = default)
        {
            Ensure.NotNullOrEmpty(userId, "userId");

            UserDailyContext candidate;
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
                candidate = await LoadUserAsync(accountsDb, userId, cancellationToken);
            }

            if (candidate == null)
                return DailyNotificationResult.NotConfigured;

            return await ProcessUserAsync(candidate, GetUtcNow(), forceSend, cancellationToken);
        }

        private async Task<DailyNotificationResult> ProcessUserAsync(UserDailyContext candidate, DateTime utcNow, bool forceSend, CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
            PushNotificationSender sender = scope.ServiceProvider.GetRequiredService<PushNotificationSender>();

            TimeZoneInfo timeZone = ResolveTimeZone(candidate.TimeZone);
            DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            DateTime localDate = localNow.Date;

            if (!forceSend && localNow.Hour < candidate.PreferredHour)
            {
                log.LogDebug("Skipping '{Topic}' for user '{UserId}': local hour {LocalHour} < preferred {PreferredHour} (tz '{TimeZone}').", TopicName, candidate.UserId, localNow.Hour, candidate.PreferredHour, timeZone.Id);
                return DailyNotificationResult.HourNotReached;
            }

            if (!forceSend)
            {
                bool alreadySent = await IsAlreadyDispatchedAsync(accountsDb, candidate.UserId, localDate, cancellationToken);

                if (alreadySent)
                {
                    log.LogDebug("Skipping '{Topic}' for user '{UserId}': already dispatched on {LocalDate:yyyy-MM-dd}.", TopicName, candidate.UserId, localDate);
                    return DailyNotificationResult.AlreadySent;
                }
            }

            TContent content = await LoadContentAsync(scope.ServiceProvider, candidate.UserId, localDate, cancellationToken);
            if (!HasContent(content))
            {
                log.LogDebug("Skipping '{Topic}' for user '{UserId}': nothing to notify about on {LocalDate:yyyy-MM-dd}.", TopicName, candidate.UserId, localDate);
                return DailyNotificationResult.NoMatches;
            }

            TDispatch dispatch = null;
            if (!forceSend)
            {
                dispatch = new TDispatch
                {
                    UserId = candidate.UserId,
                    Date = localDate,
                    Created = timeProvider.GetUtcNow().UtcDateTime
                };

                if (!await TryReserveDispatchAsync(accountsDb, dispatch, cancellationToken))
                {
                    log.LogDebug("Skipping '{Topic}' for user '{UserId}': dispatch reservation lost a race on {LocalDate:yyyy-MM-dd}.", TopicName, candidate.UserId, localDate);
                    return DailyNotificationResult.AlreadySent;
                }
            }

            List<UserNotificationPushSubscription> subscriptions = await accountsDb.PushSubscriptions
                .Where(s => s.RevokedAt == null && s.UserId == candidate.UserId)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                log.LogDebug("Releasing '{Topic}' dispatch for user '{UserId}': no active push subscriptions.", TopicName, candidate.UserId);
                await ReleaseDispatchAsync(accountsDb, dispatch, cancellationToken);
                return DailyNotificationResult.NoSubscriptions;
            }

            int delivered;
            try
            {
                delivered = await SendAsync(sender, subscriptions, content, localDate);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to send '{Topic}' notification to user '{UserId}' on {LocalDate:yyyy-MM-dd}. Releasing dispatch row.", TopicName, candidate.UserId, localDate);
                await ReleaseDispatchAsync(accountsDb, dispatch, cancellationToken);
                return DailyNotificationResult.DeliveryFailed;
            }

            if (delivered < 1)
            {
                log.LogWarning("'{Topic}' notification was not delivered to user '{UserId}' on {LocalDate:yyyy-MM-dd}. Releasing dispatch row.", TopicName, candidate.UserId, localDate);
                await ReleaseDispatchAsync(accountsDb, dispatch, cancellationToken);
                return DailyNotificationResult.DeliveryFailed;
            }

            if (dispatch != null)
            {
                dispatch.SentAt = timeProvider.GetUtcNow().UtcDateTime;
                await accountsDb.SaveChangesAsync(cancellationToken);
            }

            log.LogInformation("'{Topic}' notification delivered to user '{UserId}' on {LocalDate:yyyy-MM-dd}.", TopicName, candidate.UserId, localDate);
            return DailyNotificationResult.Sent;
        }

        private async Task<List<UserDailyContext>> LoadCandidatesAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();

            return await LoadEnabledUsersAsync(accountsDb, cancellationToken);
        }

        private async Task<bool> TryReserveDispatchAsync(AccountsDataContext accountsDb, TDispatch dispatch, CancellationToken cancellationToken)
        {
            GetDispatches(accountsDb).Add(dispatch);
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

        private async Task ReleaseDispatchAsync(AccountsDataContext accountsDb, TDispatch dispatch, CancellationToken cancellationToken)
        {
            if (dispatch == null)
                return;

            GetDispatches(accountsDb).Remove(dispatch);
            await accountsDb.SaveChangesAsync(cancellationToken);
        }

        protected static TimeZoneInfo ResolveTimeZone(string timeZone)
        {
            if (!String.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone.Trim(), out TimeZoneInfo resolved))
                return resolved;

            return TimeZoneInfo.Utc;
        }

        protected sealed record UserDailyContext(string UserId, int PreferredHour, string TimeZone);
    }
}
