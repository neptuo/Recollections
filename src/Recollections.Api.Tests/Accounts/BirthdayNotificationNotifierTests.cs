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
/// Verifies that <see cref="BirthdayNotificationNotifier"/> notifies about beings
/// whose birth date matches the user's current local day, honors the preferred
/// local hour and deduplicates per local day.
/// </summary>
public class BirthdayNotificationNotifierTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string PragueTz = "Europe/Prague";

    private readonly string AliceUserId = "bd-alice-" + Guid.NewGuid().ToString("N").Substring(0, 8);

    private readonly ApiFactory factory;

    public BirthdayNotificationNotifierTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BirthdayNotificationNotifierTests), (accountsDb, entriesDb) => Task.CompletedTask);

        using var scope = factory.Services.CreateScope();
        var accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
        var entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();

        accountsDb.NotificationBirthdayDispatches.RemoveRange(accountsDb.NotificationBirthdayDispatches);
        accountsDb.NotificationBirthdaySettings.RemoveRange(accountsDb.NotificationBirthdaySettings);
        accountsDb.NotificationSettings.RemoveRange(accountsDb.NotificationSettings);
        accountsDb.PushSubscriptions.RemoveRange(accountsDb.PushSubscriptions);
        entriesDb.Beings.RemoveRange(entriesDb.Beings);
        await accountsDb.SaveChangesAsync();
        await entriesDb.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HourNotReached_DoesNotSend()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 9, timeZone: PragueTz);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 15));

        // 06:00 Prague on 2025-06-15 → 04:00 UTC (summer, +2h offset).
        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 4, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(host.Sender.Sent);
        Assert.Empty(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task BirthdayToday_SendsOnceWithAgeAndRecordsDispatch()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        var sent = Assert.Single(host.Sender.Sent);
        var being = Assert.Single(sent.Beings);
        Assert.Equal("Bob", being.Name);
        Assert.Equal(35, being.Age);
        Assert.Equal(new DateTime(2025, 6, 15), sent.LocalDate);

        var dispatch = Assert.Single(await QueryDispatchesAsync(host, AliceUserId));
        Assert.Equal(new DateTime(2025, 6, 15), dispatch.Date);
        Assert.NotNull(dispatch.SentAt);
    }

    [Fact]
    public async Task NoBirthdayToday_DoesNotSend()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 16));
        await SeedBeingAsync(host, AliceUserId, "NoBirthDate", null);

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(host.Sender.Sent);
        Assert.Empty(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task SecondTickSameLocalDay_DoesNotSendAgain()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 15, 0, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Single(host.Sender.Sent);
        Assert.Single(await QueryDispatchesAsync(host, AliceUserId));
    }

    [Fact]
    public async Task MultipleBeings_AreSentTogether()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 15));
        await SeedBeingAsync(host, AliceUserId, "Carol", new DateTime(2000, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        var sent = Assert.Single(host.Sender.Sent);
        Assert.Equal(new[] { "Bob", "Carol" }, sent.Beings.Select(b => b.Name).ToArray());
    }

    [Fact]
    public async Task MasterSwitchDisabled_DoesNotSend()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 8, timeZone: PragueTz, masterEnabled: false);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 6, 30, 0, TimeSpan.Zero));
        await host.Notifier.RunAsync();

        Assert.Empty(host.Sender.Sent);
    }

    [Fact]
    public async Task ForceSend_SkipsHourGateAndDoesNotRecordDispatch()
    {
        await using var host = CreateHost();
        await SeedEnabledUserAsync(host, AliceUserId, preferredHour: 23, timeZone: PragueTz);
        await SeedBeingAsync(host, AliceUserId, "Bob", new DateTime(1990, 6, 15));

        host.FakeTime.SetUtcNow(new DateTimeOffset(2025, 6, 15, 4, 0, 0, TimeSpan.Zero));
        var result = await host.Notifier.RunForUserAsync(AliceUserId, forceSend: true);

        Assert.Equal(DailyNotificationResult.Sent, result);
        Assert.Single(host.Sender.Sent);
        Assert.Empty(await QueryDispatchesAsync(host, AliceUserId));
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

        _ = customized.Services;

        var notifier = customized.Services.GetRequiredService<BirthdayNotificationNotifier>();
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
        accountsDb.NotificationBirthdaySettings.Add(new UserNotificationBirthdaySettings
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

    private static async Task SeedBeingAsync(TestHost host, string userId, string name, DateTime? birthDate)
    {
        using var scope = host.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();
        db.Beings.Add(new Being
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 32),
            UserId = userId,
            Name = name,
            BirthDate = birthDate,
            Created = DateTime.UtcNow,
            IsSharingInherited = false
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<UserNotificationBirthdayDispatch>> QueryDispatchesAsync(TestHost host, string userId)
    {
        using var scope = host.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
        return await db.NotificationBirthdayDispatches
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    private sealed class TestHost : IAsyncDisposable
    {
        public WebApplicationFactory<Program> Factory { get; }
        public FakeTimeProvider FakeTime { get; }
        public RecordingPushSender Sender { get; }
        public BirthdayNotificationNotifier Notifier { get; }

        public TestHost(WebApplicationFactory<Program> factory, FakeTimeProvider fakeTime, RecordingPushSender sender, BirthdayNotificationNotifier notifier)
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

        public override bool IsConfigured => true;

        public override Task<int> SendBirthdayAsync(IEnumerable<UserNotificationPushSubscription> subscriptions, IReadOnlyCollection<BirthdayNotificationItem> beings, DateTime localDate)
        {
            var list = subscriptions.ToList();
            Sent.Add(new SentRecord(list.First().UserId, beings.ToList(), localDate));
            return Task.FromResult(list.Count);
        }

        public sealed record SentRecord(string UserId, IReadOnlyList<BirthdayNotificationItem> Beings, DateTime LocalDate);
    }
}
