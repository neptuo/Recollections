using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Notifications;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Tests.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Tests.Accounts;

/// <summary>
/// Verifies that <see cref="OnThisDayNotificationNotifier"/> honors the user's
/// preferred local hour + timezone, deduplicates per local day, survives
/// per-user failures, and handles DST transitions.
/// </summary>
public class OnThisDayNotificationNotifierTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string PragueTz = "Europe/Prague";
    private const string InvalidTz = "Not/AZone";

    private readonly string AliceUserId = "otd-alice-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly string BobUserId = "otd-bob-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly string CarolUserId = "otd-carol-" + Guid.NewGuid().ToString("N").Substring(0, 8);

    private readonly ApiFactory factory;

    public OnThisDayNotificationNotifierTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(OnThisDayNotificationNotifierTests), (accountsDb, entriesDb) => Task.CompletedTask);

        // Each test needs a clean slate even though the SQLite DB is shared across the class fixture.
        using var scope = factory.Services.CreateScope();
        var accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
        var entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();

        accountsDb.NotificationOnThisDayDispatches.RemoveRange(accountsDb.NotificationOnThisDayDispatches);
        accountsDb.NotificationOnThisDaySettings.RemoveRange(accountsDb.NotificationOnThisDaySettings);
        accountsDb.NotificationSettings.RemoveRange(accountsDb.NotificationSettings);
        accountsDb.PushSubscriptions.RemoveRange(accountsDb.PushSubscriptions);
        entriesDb.Entries.RemoveRange(entriesDb.Entries);
        await accountsDb.SaveChangesAsync();
        await entriesDb.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string NewUserId() => "otd-" + Guid.NewGuid().ToString("N").Substring(0, 10);

    [Fact]
    public async Task HourNotReached_DoesNotSend()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 9, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        // 06:00 Prague on 2025-06-15 → 04:00 UTC (summer, +2h offset).
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 4, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(host.Sender.Sent);
        Assert.Empty(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task HourReached_SendsOnceAndRecordsDispatch()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        // 08:30 Prague (CEST) → 06:30 UTC.
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        var sent = Assert.Single(host.Sender.Sent);
        Assert.Equal(1, sent.EntryCount);
        Assert.Equal(new DateTime(2025, 6, 15), sent.LocalDate);

        var dispatches = await QueryDispatchesAsync(host, AliceUserId);
        var dispatch = Assert.Single(dispatches);
        Assert.Equal(new DateTime(2025, 6, 15), dispatch.Date);
        Assert.NotNull(dispatch.SentAt);
    }

    [Fact]
    public async Task SecondTickSameLocalDay_DoesNotSendAgain()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 15, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Single(host.Sender.Sent);
        Assert.Single(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task NextLocalDay_SendsAgain()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2021, 6, 16));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 16, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Equal(2, host.Sender.Sent.Count);
        Assert.Equal(2, (await QueryDispatchesAsync(host, AliceUserId)).Count);
    }

    [Fact]
    public async Task DstSpringForward_UsesLocalTime()
    {
        // Prague DST starts 2025-03-30 02:00 → 03:00 local. At 07:00 UTC it's 09:00 CEST.
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2024, 3, 30));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 3, 30, 7, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Single(host.Sender.Sent);
    }

    [Fact]
    public async Task DstFallBack_DoesNotDoubleSend()
    {
        // Prague DST ends 2025-10-26 03:00 local → back to 02:00 local (ambiguous hour).
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 10, 26));

        // 08:00 CEST → 06:00 UTC on 2025-10-26.
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 10, 26, 6, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();
        // After the roll-back, 08:00 CET → 07:00 UTC same local day.
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 10, 26, 7, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Single(host.Sender.Sent);
        Assert.Single(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task InvalidTimeZone_FallsBackToUtc()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: InvalidTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 8, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Single(host.Sender.Sent);
    }

    [Fact]
    public async Task MasterDisabled_DoesNotSend()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz, masterEnabled: false);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(host.Sender.Sent);
    }

    [Fact]
    public async Task NoActiveSubscription_DoesNotSend()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz, withSubscription: false);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(host.Sender.Sent);
    }

    [Fact]
    public async Task DeliveryReturnsZero_ReleasesDispatchForRetry()
    {
        await using var host = CreateHost();
        host.Sender.DeliveredOverride = 0;
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 7, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(await QueryDispatchesAsync(host, AliceUserId));

        host.Sender.DeliveredOverride = null;
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 8, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Equal(2, host.Sender.Sent.Count);
        Assert.Single(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task OneUserThrows_OtherUsersStillProcessed()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedEnabledUserAsync(host, BobUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));
        await SeedAnniversaryEntryAsync(host, BobUserId, new DateTime(2021, 6, 15));

        host.Sender.ThrowForUserId = AliceUserId;
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Contains(host.Sender.Sent, s => s.UserId == BobUserId);
        Assert.DoesNotContain(host.Sender.Sent, s => s.UserId == AliceUserId);
        Assert.Single(await QueryDispatchesAsync(host, BobUserId));
    }

    [Fact]
    public async Task RunForUserAsync_Force_BypassesHourAndDedupe()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 23, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        // 06:00 local — well before PreferredHour 23.
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 4, 0, 0, TimeSpan.Zero));
        var result = await host.Notifier.RunForUserAsync(AliceUserId, forceSend: true);

        Assert.Equal(OnThisDayTestResult.Sent, result);
        Assert.Single(host.Sender.Sent);
        Assert.Empty(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task ClockOffset_Honored()
    {
        await using var host = CreateHost(o => o.OnThisDay.ClockOffset = TimeSpan.FromHours(6));
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedAnniversaryEntryAsync(host, AliceUserId, new DateTime(2020, 6, 15));

        // Real UTC 02:00 + 6h offset = evaluated UTC 08:00 → Prague 10:00.
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 2, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Single(host.Sender.Sent);
    }

    // --------- helpers ---------

    private TestHost CreateHost(Action<NotificationOptions> configureOptions = null)
    {
        var fakeTime = new FakeTimeProvider();
        var recordingSender = new RecordingPushSender();

        WebApplicationFactory<Program> customized = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Disable hosted services so the notifier does not race with manual runs.
                services.RemoveAll<IHostedService>();

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(fakeTime);

                services.RemoveAll<PushNotificationSender>();
                services.AddSingleton<PushNotificationSender>(recordingSender);

                services.Configure<NotificationOptions>(o =>
                {
                    o.Subject = "mailto:tests@recollections.local";
                    o.PublicKey = "test-public";
                    o.PrivateKey = "test-private";
                    configureOptions?.Invoke(o);
                });
            });
        });

        // Force host creation so the overrides above are applied.
        _ = customized.Services;

        var notifier = customized.Services.GetRequiredService<OnThisDayNotificationNotifier>();
        return new TestHost(customized, fakeTime, recordingSender, notifier);
    }

    private static async Task SeedEnabledUserAsync(TestHost host, string userId, int preferredHour, string timeZone, bool masterEnabled = true, bool withSubscription = true)
    {
        using var scope = host.Factory.Services.CreateScope();
        var accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
        var entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();

        await DatabaseSeeder.SeedUser(accountsDb, userId, userId);
        await DatabaseSeeder.SeedUserBeing(entriesDb, userId, userId);

        accountsDb.NotificationSettings.Add(new UserNotificationSettings { UserId = userId, IsEnabled = masterEnabled });
        accountsDb.NotificationOnThisDaySettings.Add(new UserNotificationOnThisDaySettings
        {
            UserId = userId,
            IsEnabled = true,
            PreferredHour = preferredHour,
            TimeZone = timeZone
        });

        if (withSubscription)
        {
            accountsDb.PushSubscriptions.Add(new UserNotificationPushSubscription
            {
                UserId = userId,
                Endpoint = $"https://push.test/{userId}",
                P256dh = "p256",
                Auth = "auth",
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            });
        }

        await accountsDb.SaveChangesAsync();
    }

    private static async Task SeedAnniversaryEntryAsync(TestHost host, string userId, DateTime when)
    {
        using var scope = host.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();
        var entry = new Entry
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 32),
            UserId = userId,
            Title = "Anniversary",
            When = when,
            Created = when,
            IsSharingInherited = false
        };
        db.Entries.Add(entry);
        await db.SaveChangesAsync();
    }

    private static async Task<List<UserNotificationOnThisDayDispatch>> QueryDispatchesAsync(TestHost host, string userId)
    {
        using var scope = host.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
        return await db.NotificationOnThisDayDispatches
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    private sealed class TestHost : IAsyncDisposable
    {
        public WebApplicationFactory<Program> Factory { get; }
        public FakeTimeProvider FakeTime { get; }
        public RecordingPushSender Sender { get; }
        public OnThisDayNotificationNotifier Notifier { get; }

        public TestHost(WebApplicationFactory<Program> factory, FakeTimeProvider fakeTime, RecordingPushSender sender, OnThisDayNotificationNotifier notifier)
        {
            Factory = factory;
            FakeTime = fakeTime;
            Sender = sender;
            Notifier = notifier;
        }

        public ValueTask DisposeAsync()
        {
            Factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingPushSender : PushNotificationSender
    {
        public RecordingPushSender()
            : base(
                new WebPush.WebPushClient(),
                Options.Create(new NotificationOptions
                {
                    Subject = "mailto:tests@recollections.local",
                    PublicKey = "test-public",
                    PrivateKey = "test-private"
                }),
                NullLogger<PushNotificationSender>.Instance)
        {
        }

        public List<SentRecord> Sent { get; } = new();
        public int? DeliveredOverride { get; set; }
        public string ThrowForUserId { get; set; }

        public override bool IsConfigured => true;

        public override Task<int> SendOnThisDayAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, int entryCount, DateTime localDate)
        {
            var list = subscriptions.ToList();
            var userId = list.First().UserId;
            if (ThrowForUserId != null && userId == ThrowForUserId)
                throw new InvalidOperationException("Simulated push failure.");

            Sent.Add(new SentRecord(userId, entryCount, localDate));
            return Task.FromResult(DeliveredOverride ?? list.Count);
        }

        public sealed record SentRecord(string UserId, int EntryCount, DateTime LocalDate);
    }
}
