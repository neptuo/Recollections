using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public class OnThisDayNotificationBackgroundService : BackgroundService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<OnThisDayNotificationBackgroundService> log;

        public OnThisDayNotificationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OnThisDayNotificationBackgroundService> log)
        {
            this.scopeFactory = scopeFactory;
            this.log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            log.LogInformation("'On this day' notification background service starting. Tick interval: {TickInterval}.", TickInterval);

            using PeriodicTimer timer = new(TickInterval);
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
            await notifier.RunAsync(DateTime.UtcNow, stoppingToken);
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
