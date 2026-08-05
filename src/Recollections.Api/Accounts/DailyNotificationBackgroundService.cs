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
    /// <summary>
    /// Periodically ticks a notifier that is evaluated once per user's local day.
    /// </summary>
    public abstract class DailyNotificationBackgroundService<TNotifier> : BackgroundService
        where TNotifier : notnull
    {
        private static readonly TimeSpan MinTickInterval = TimeSpan.FromSeconds(1);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly TimeProvider timeProvider;
        private readonly IOptionsMonitor<NotificationOptions> options;
        private readonly ILogger log;

        protected DailyNotificationBackgroundService(
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

        protected abstract string TopicName { get; }

        protected abstract string TickIntervalConfigurationKey { get; }

        protected abstract TimeSpan GetConfiguredTickInterval(NotificationOptions options);

        protected abstract Task RunAsync(TNotifier notifier, CancellationToken stoppingToken);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan tickInterval = ResolveTickInterval();
            log.LogInformation("'{Topic}' notification background service starting. Tick interval: {TickInterval}.", TopicName, tickInterval);

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
                    log.LogError(ex, "'{Topic}' notification tick failed.", TopicName);
                }
            }
            while (await WaitForNextTickAsync(timer, stoppingToken));
        }

        private async Task RunTickAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            TNotifier notifier = scope.ServiceProvider.GetRequiredService<TNotifier>();
            await RunAsync(notifier, stoppingToken);
        }

        private TimeSpan ResolveTickInterval()
        {
            TimeSpan configured = GetConfiguredTickInterval(options.CurrentValue);
            if (configured <= TimeSpan.Zero)
            {
                log.LogWarning("'{Topic}' tick interval is not configured ({ConfigurationKey}). Falling back to 15 minutes.", TopicName, TickIntervalConfigurationKey);
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
