using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptuo;
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
    public class NewEntriesNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly NotificationOptions options;
        private readonly ILogger<NewEntriesNotificationBackgroundService> log;

        public NewEntriesNotificationBackgroundService(IServiceScopeFactory scopeFactory, IOptions<NotificationOptions> options, ILogger<NewEntriesNotificationBackgroundService> log)
        {
            Ensure.NotNull(scopeFactory, "scopeFactory");
            Ensure.NotNull(options, "options");
            Ensure.NotNull(log, "log");
            this.scopeFactory = scopeFactory;
            this.options = options.Value;
            this.log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan period = TimeSpan.FromMinutes(Math.Max(1, options.CheckPeriodMinutes));
            using PeriodicTimer timer = new(period);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failed to process notification digests.");
                }

                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
        }

        private async Task ProcessAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AccountsDataContext accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
            EntriesDataContext entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();
            IConnectionProvider connections = scope.ServiceProvider.GetRequiredService<IConnectionProvider>();
            ShareStatusService shareStatus = scope.ServiceProvider.GetRequiredService<ShareStatusService>();
            PushNotificationSender sender = scope.ServiceProvider.GetRequiredService<PushNotificationSender>();

            if (!sender.IsConfigured)
                return;

            var globalSettings = await accountsDb.NotificationSettings
                .Where(s => s.IsEnabled)
                .Select(s => new { s.UserId, s.TimeZoneId, s.PreferredHour })
                .ToListAsync(cancellationToken);

            HashSet<string> topicEnabledUsers = (await accountsDb.NotificationNewEntriesSettings
                .Where(s => s.IsEnabled)
                .Select(s => s.UserId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            HashSet<string> subscribedUsers = (await accountsDb.PushSubscriptions
                .Where(s => s.RevokedAt == null)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            DateTime utcNow = DateTime.UtcNow;
            foreach (var globalSetting in globalSettings)
            {
                if (!topicEnabledUsers.Contains(globalSetting.UserId) || !subscribedUsers.Contains(globalSetting.UserId))
                    continue;

                TimeZoneInfo timeZone = GetTimeZone(globalSetting.TimeZoneId);
                DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
                int preferredHour = Math.Max(0, Math.Min(23, globalSetting.PreferredHour));
                if (localNow.Hour < preferredHour)
                    continue;

                DateTime localDate = localNow.Date;
                bool alreadySentToday = await accountsDb.NotificationDispatches
                    .AnyAsync(
                        d => d.UserId == globalSetting.UserId
                            && d.Kind == (int)NotificationDispatchKind.NewEntries
                            && d.LocalDate == localDate,
                        cancellationToken
                    );

                if (alreadySentToday)
                    continue;

                NotificationDispatch lastDispatch = await accountsDb.NotificationDispatches
                    .Where(d => d.UserId == globalSetting.UserId && d.Kind == (int)NotificationDispatchKind.NewEntries)
                    .OrderByDescending(d => d.SentAt)
                    .FirstOrDefaultAsync(cancellationToken);

                DateTime createdAfter = lastDispatch?.SentAt ?? DateTime.Now.AddDays(-1);
                ConnectedUsersModel connectedUsers = await connections.GetConnectedUsersForAsync(globalSetting.UserId);

                int newEntriesCount = await shareStatus
                    .OwnedByOrExplicitlySharedWithUser(
                        entriesDb,
                        entriesDb.Entries.Where(e => e.Created > createdAfter),
                        globalSetting.UserId,
                        connectedUsers
                    )
                    .Where(e => e.UserId != globalSetting.UserId)
                    .CountAsync(cancellationToken);

                if (newEntriesCount < 1)
                    continue;

                List<UserNotificationPushSubscription> subscriptions = await accountsDb.PushSubscriptions
                    .Where(s => s.UserId == globalSetting.UserId && s.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                int deliveredCount = await sender.SendNewEntriesAsync(subscriptions, newEntriesCount);
                if (deliveredCount > 0)
                {
                    accountsDb.NotificationDispatches.Add(new NotificationDispatch()
                    {
                        UserId = globalSetting.UserId,
                        Kind = (int)NotificationDispatchKind.NewEntries,
                        LocalDate = localDate,
                        SentAt = DateTime.Now
                    });
                }

                if (accountsDb.ChangeTracker.HasChanges())
                    await accountsDb.SaveChangesAsync(cancellationToken);
            }
        }

        private TimeZoneInfo GetTimeZone(string timeZoneId)
        {
            if (String.IsNullOrWhiteSpace(timeZoneId))
                timeZoneId = options.DefaultTimeZoneId;

            if (String.IsNullOrWhiteSpace(timeZoneId))
                return TimeZoneInfo.Utc;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                log.LogWarning(ex, "Unknown timezone '{TimeZoneId}', falling back to UTC.", timeZoneId);
                return TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException ex)
            {
                log.LogWarning(ex, "Invalid timezone '{TimeZoneId}', falling back to UTC.", timeZoneId);
                return TimeZoneInfo.Utc;
            }
        }
    }
}
