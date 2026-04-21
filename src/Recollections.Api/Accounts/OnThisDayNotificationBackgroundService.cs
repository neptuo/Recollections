using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public class OnThisDayNotificationBackgroundService : BackgroundService
    {
        private static readonly TimeSpan MinTickInterval = TimeSpan.FromSeconds(1);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly TimeProvider timeProvider;
        private readonly IOptionsMonitor<NotificationOptions> options;
        private readonly ILogger<OnThisDayNotificationBackgroundService> log;

        public OnThisDayNotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IOptionsMonitor<NotificationOptions> options,
            ILogger<OnThisDayNotificationBackgroundService> log)
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan tickInterval = ResolveTickInterval();
            log.LogInformation("'On this day' notification background service starting. Tick interval: {TickInterval}.", tickInterval);

            using PeriodicTimer timer = new(tickInterval, timeProvider);
            do
            {
                try
                {
                    await RunTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "'On this day' notification tick failed.");
                }
            }
            while (await WaitForNextTickAsync(timer, stoppingToken));
        }

        private async Task RunTickAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            OnThisDayNotificationNotifier notifier = scope.ServiceProvider.GetRequiredService<OnThisDayNotificationNotifier>();
            await notifier.RunAsync(stoppingToken);
        }

        private TimeSpan ResolveTickInterval()
        {
            TimeSpan configured = options.CurrentValue?.OnThisDay?.TickInterval ?? TimeSpan.Zero;
            if (configured <= TimeSpan.Zero)
            {
                log.LogWarning("'On this day' tick interval is not configured (Accounts:Notifications:OnThisDay:TickInterval). Falling back to 15 minutes.");
                configured = TimeSpan.FromMinutes(15);
            }
            return configured < MinTickInterval ? MinTickInterval : configured;
        }

        private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
        {
            try
            {
                return await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
