using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public class BirthdayNotificationBackgroundService : DailyNotificationBackgroundService<BirthdayNotificationNotifier>
    {
        public BirthdayNotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IOptionsMonitor<NotificationOptions> options,
            ILogger<BirthdayNotificationBackgroundService> log)
            : base(scopeFactory, timeProvider, options, log)
        { }

        protected override string TopicName => "Birthday";

        protected override string TickIntervalConfigurationKey => "Accounts:Notifications:Birthday:TickInterval";

        protected override TimeSpan GetConfiguredTickInterval(NotificationOptions options)
            => options?.Birthday?.TickInterval ?? TimeSpan.Zero;

        protected override Task RunAsync(BirthdayNotificationNotifier notifier, CancellationToken stoppingToken)
            => notifier.RunAsync(stoppingToken);
    }
}
