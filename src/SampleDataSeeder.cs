#:sdk Microsoft.NET.Sdk
#:property PublishAot=false
#:project ./Recollections.Api/Recollections.Api.csproj
#:project ./Recollections.Accounts/Recollections.Accounts.csproj
#:project ./Recollections.Accounts.Data/Recollections.Accounts.Data.csproj
#:project ./Recollections.Data.Ef/Recollections.Data.Ef.csproj
#:project ./Recollections.Entries/Recollections.Entries.csproj
#:project ./Recollections.Entries.Data/Recollections.Entries.Data.csproj
#:project ./Recollections.Entries.SystemIo/Recollections.Entries.SystemIo.csproj
#:project ./Recollections.SystemIo/Recollections.SystemIo.csproj

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neptuo.Recollections;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;
using Permission = Neptuo.Recollections.Sharing.Permission;

await new SampleDataSeeder().RunAsync(args);

internal sealed class SampleDataSeeder
{
    private const string DefaultPassword = "demo1234";
    private static readonly string[] SampleUserNames = ["jondoe", "janedoe", "billdoe"];
    private static readonly SampleUserDefinition[] Users =
    [
        new("jondoe", "jondoe@example.com", IsPremium: true),
        new("janedoe", "janedoe@example.com", IsPremium: false),
        new("billdoe", "billdoe@example.com", IsPremium: false)
    ];

    private static readonly SampleStoryDefinition[] Stories =
    [
        new(
            OwnerUserName: "jondoe",
            Title: "Spring in Prague",
            Text: "A relaxed week of river walks, coffee stops, and golden-hour photos around the city.",
            IsSharingInherited: true,
            SharedWithUserNames: [],
            Entries:
            [
                new(
                    Title: "Golden hour on Charles Bridge",
                    When: new DateTime(2025, 3, 15, 18, 30, 0, DateTimeKind.Local),
                    Text: "Caught the last light over the river and stopped for a few photos before dinner.",
                    MediaCount: 3,
                    Locations:
                    [
                        new(50.086520, 14.411350, 192),
                        new(50.087580, 14.420790, 188),
                        new(50.089860, 14.404020, 195)
                    ],
                    BeingNames: ["Jon", "Max"]
                ),
                new(
                    Title: "Coffee at Letna",
                    When: new DateTime(2025, 3, 16, 10, 0, 0, DateTimeKind.Local),
                    Text: "A quiet morning, fresh pastries, and a notebook full of ideas.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(50.097120, 14.417370, 260)
                    ],
                    BeingNames: ["Jon"]
                )
            ]
        ),
        new(
            OwnerUserName: "jondoe",
            Title: "Family weekend getaway",
            Text: "Small moments from an easy road trip outside the city.",
            IsSharingInherited: true,
            SharedWithUserNames: [],
            Entries:
            [
                new(
                    Title: "Castle walk in Kutna Hora",
                    When: new DateTime(2025, 2, 8, 11, 15, 0, DateTimeKind.Local),
                    Text: "Brisk weather, quiet streets, and a long loop around the old center.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(49.948540, 15.268210, 254)
                    ],
                    BeingNames: ["Jon", "Max"]
                ),
                new(
                    Title: "Sunday lunch by the square",
                    When: new DateTime(2025, 2, 9, 13, 0, 0, DateTimeKind.Local),
                    Text: "Wrapped up the trip with soup, shared dessert, and one last short walk.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(49.947170, 15.268410, 252)
                    ],
                    BeingNames: ["Jon"]
                )
            ]
        ),
        new(
            OwnerUserName: "jondoe",
            Title: "Morning rides",
            Text: "Short bike rides before the day starts, mostly along the river and up nearby hills.",
            IsSharingInherited: true,
            SharedWithUserNames: [],
            Entries:
            [
                new(
                    Title: "Ride along the river",
                    When: new DateTime(2024, 9, 3, 6, 50, 0, DateTimeKind.Local),
                    Text: "The path was nearly empty and the whole city was still waking up.",
                    MediaCount: 0,
                    Locations:
                    [
                        new(50.067820, 14.414940, 185),
                        new(50.075160, 14.431640, 190)
                    ],
                    BeingNames: ["Jon"]
                ),
                new(
                    Title: "Hilltop overlook",
                    When: new DateTime(2024, 9, 10, 7, 10, 0, DateTimeKind.Local),
                    Text: "Stopped at the top for a quick drink and a wide view over the rooftops.",
                    MediaCount: 0,
                    Locations:
                    [
                        new(50.087640, 14.389840, 355)
                    ],
                    BeingNames: ["Jon", "Max"]
                )
            ]
        ),
        new(
            OwnerUserName: "jondoe",
            Title: "Shared product notes",
            Text: "A few moments from planning sessions that were worth keeping together.",
            IsSharingInherited: false,
            SharedWithUserNames: ["janedoe"],
            Entries:
            [
                new(
                    Title: "Whiteboard sketch session",
                    When: new DateTime(2025, 1, 21, 9, 0, 0, DateTimeKind.Local),
                    Text: "The rough sketch that finally made the release plan click.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(50.081300, 14.428100, 210)
                    ]
                ),
                new(
                    Title: "Polished the release plan",
                    When: new DateTime(2025, 1, 22, 14, 20, 0, DateTimeKind.Local),
                    Text: "Turned sticky-note chaos into a calm, readable milestone list.",
                    MediaCount: 0,
                    Locations:
                    [
                        new(50.080710, 14.427380, 208)
                    ]
                )
            ]
        ),
        new(
            OwnerUserName: "janedoe",
            Title: "Design walk in Vienna",
            Text: "Museum courtyards, pastries, and a lot of photo references for later.",
            IsSharingInherited: true,
            SharedWithUserNames: [],
            Entries:
            [
                new(
                    Title: "Notebook and pastries",
                    When: new DateTime(2025, 4, 6, 9, 45, 0, DateTimeKind.Local),
                    Text: "Started the morning slowly with coffee and a quick design check-in.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(48.208490, 16.372080, 171)
                    ],
                    BeingNames: ["Jane"]
                ),
                new(
                    Title: "Museum courtyard palettes",
                    When: new DateTime(2025, 4, 6, 15, 10, 0, DateTimeKind.Local),
                    Text: "Collected colors, textures, and a few framing ideas for the next project.",
                    MediaCount: 2,
                    Locations:
                    [
                        new(48.203790, 16.361550, 175)
                    ],
                    BeingNames: ["Jane"]
                )
            ]
        ),
        new(
            OwnerUserName: "janedoe",
            Title: "Seaside sketches",
            Text: "Fast studies from a short trip to the coast, mostly wind, light, and quick notes.",
            IsSharingInherited: true,
            SharedWithUserNames: [],
            Entries:
            [
                new(
                    Title: "Harbor sunrise",
                    When: new DateTime(2024, 7, 14, 6, 20, 0, DateTimeKind.Local),
                    Text: "The boats were quiet, the sky was soft, and the whole place felt slow.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(43.695120, 7.276360, 5)
                    ],
                    BeingNames: ["Jane"]
                ),
                new(
                    Title: "Late afternoon by the promenade",
                    When: new DateTime(2024, 7, 14, 17, 35, 0, DateTimeKind.Local),
                    Text: "Wrapped the day with a few sketches and one long walk by the water.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(43.710170, 7.261950, 12)
                    ],
                    BeingNames: ["Jane"]
                )
            ]
        ),
        new(
            OwnerUserName: "janedoe",
            Title: "Weekend market finds",
            Text: "Flowers, ceramics, and a long list of little things worth remembering.",
            IsSharingInherited: false,
            SharedWithUserNames: ["jondoe"],
            Entries:
            [
                new(
                    Title: "Ceramics and flowers",
                    When: new DateTime(2025, 3, 29, 10, 30, 0, DateTimeKind.Local),
                    Text: "Came home with far more flowers than planned and zero regrets.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(48.210760, 16.370440, 168)
                    ],
                    BeingNames: ["Jane"]
                ),
                new(
                    Title: "Fresh fruit and postcards",
                    When: new DateTime(2025, 3, 29, 11, 15, 0, DateTimeKind.Local),
                    Text: "A small stack of postcards, berries, and one very good loaf of bread.",
                    MediaCount: 0,
                    Locations:
                    [
                        new(48.211850, 16.369110, 170)
                    ],
                    BeingNames: ["Jane"]
                )
            ]
        ),
        new(
            OwnerUserName: "billdoe",
            Title: "Neighborhood photo hunt",
            Text: "Short walks with a camera after the rain, mostly looking for reflections and color.",
            IsSharingInherited: false,
            SharedWithUserNames: ["jondoe"],
            Entries:
            [
                new(
                    Title: "Murals after rain",
                    When: new DateTime(2025, 2, 3, 18, 10, 0, DateTimeKind.Local),
                    Text: "The sidewalks were still wet enough to double every bright color in view.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(47.606460, -122.332750, 50)
                    ],
                    BeingNames: ["Bill"]
                ),
                new(
                    Title: "Street corner reflections",
                    When: new DateTime(2025, 2, 3, 18, 50, 0, DateTimeKind.Local),
                    Text: "Waited for the light to change and kept shooting the same corner from three angles.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(47.608610, -122.335920, 55)
                    ],
                    BeingNames: ["Bill"]
                )
            ]
        ),
        new(
            OwnerUserName: "billdoe",
            Title: "Studio setup diary",
            Text: "Small steps toward a calmer workspace with better light and fewer cables everywhere.",
            IsSharingInherited: false,
            SharedWithUserNames: ["jondoe"],
            Entries:
            [
                new(
                    Title: "Desk rewire complete",
                    When: new DateTime(2024, 11, 12, 20, 15, 0, DateTimeKind.Local),
                    Text: "Finally hid the last messy bundle of cables and it feels much better now.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(45.515290, -122.678410, 15)
                    ]
                ),
                new(
                    Title: "Testing the light setup",
                    When: new DateTime(2024, 11, 13, 19, 40, 0, DateTimeKind.Local),
                    Text: "The softer light helped immediately, especially for quick desk photos.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(45.516580, -122.681120, 18)
                    ]
                )
            ]
        ),
        new(
            OwnerUserName: "billdoe",
            Title: "City breaks",
            Text: "A compact notebook of favorite moments from a few fast-paced city weekends.",
            IsSharingInherited: false,
            SharedWithUserNames: ["jondoe", "janedoe"],
            Entries:
            [
                new(
                    Title: "Arrival in Lisbon",
                    When: new DateTime(2024, 5, 4, 16, 5, 0, DateTimeKind.Local),
                    Text: "Dropped the bags, opened the window, and went straight back outside.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(38.712620, -9.139140, 42)
                    ],
                    BeingNames: ["Bill"]
                ),
                new(
                    Title: "Sunset tram ride",
                    When: new DateTime(2024, 5, 5, 19, 10, 0, DateTimeKind.Local),
                    Text: "The tram was full, the windows were warm, and the whole ride felt cinematic.",
                    MediaCount: 1,
                    Locations:
                    [
                        new(38.714980, -9.143430, 78)
                    ],
                    BeingNames: ["Bill"]
                )
            ]
        )
    ];

    private static readonly SampleBeingDefinition[] Beings =
    [
        new(
            OwnerUserName: "jondoe",
            Name: "Jon",
            Icon: "user",
            Text: "Main personal profile."
        ),
        new(
            OwnerUserName: "jondoe",
            Name: "Max",
            Icon: "dog",
            Text: "Family dog, always on the trail."
        ),
        new(
            OwnerUserName: "janedoe",
            Name: "Jane",
            Icon: "female",
            Text: "Design trips and market finds."
        ),
        new(
            OwnerUserName: "billdoe",
            Name: "Bill",
            Icon: "male",
            Text: "Photos, studio work, and city breaks."
        )
    ];

    public async Task RunAsync(string[] args)
    {
        var options = SeedOptions.Parse(args);
        string repositoryRoot = FindRepositoryRoot();
        string apiRoot = Path.Combine(repositoryRoot, "src", "Recollections.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration, apiRoot);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var accountsDb = serviceProvider.GetRequiredService<AccountsDataContext>();
        var entriesDb = serviceProvider.GetRequiredService<EntriesDataContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var imageService = serviceProvider.GetRequiredService<ImageService>();

        await accountsDb.Database.MigrateAsync();
        await entriesDb.Database.MigrateAsync();

        var existingUsers = await accountsDb.Users
            .Where(u => SampleUserNames.Contains(u.UserName))
            .ToListAsync();

        await DeleteExistingSampleContentAsync(entriesDb, existingUsers.Select(u => u.Id).ToArray(), repositoryRoot);
        await DeleteExistingAccountsStateAsync(accountsDb, userManager, existingUsers);

        var users = await CreateUsersAsync(userManager);
        await EnsureConnectionAsync(accountsDb, users["jondoe"].Id, users["janedoe"].Id);

        string mediaRoot = options.ResolveMediaDirectory(repositoryRoot);
        var mediaFiles = GetMediaFiles(mediaRoot, repositoryRoot);
        var beingsByName = await SeedBeingsAsync(entriesDb, users);
        await SeedStoriesAsync(entriesDb, imageService, users, mediaFiles, beingsByName);

        Console.WriteLine();
        Console.WriteLine("Sample data seeded successfully.");
        Console.WriteLine($"Source media: {mediaRoot}");
        Console.WriteLine($"Credentials: {string.Join(", ", Users.Select(u => $"{u.UserName}/{DefaultPassword}"))}");
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, string apiRoot)
    {
        PathResolver pathResolver = relativePath => relativePath.Replace("{BasePath}", apiRoot, StringComparison.Ordinal);

        services
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<PathResolver>(pathResolver)
            .AddDbContextWithSchema<AccountsDataContext>(configuration.GetSection("Accounts").GetSection("Database"), pathResolver)
            .AddDbContextWithSchema<EntriesDataContext>(configuration.GetSection("Entries").GetSection("Database"), pathResolver)
            .AddIdentityCore<User>(options => configuration.GetSection("Accounts").GetSection("Identity").GetSection("Password").Bind(options.Password))
            .AddEntityFrameworkStores<AccountsDataContext>();

        services
            .AddTransient<IUserPremiumProvider, DbUserPremiumProvider>()
            .AddTransient<IImageValidator, PremiumImageSizeValidator>()
            .AddTransient<FreeLimitsChecker>()
            .AddTransient<ImageResizeService>()
            .AddTransient<ImageService>()
            .AddTransient<IFileStorage, SystemIoFileStorage>()
            .Configure<StorageOptions>(configuration.GetSection("Entries").GetSection("Storage"))
            .Configure<SystemIoStorageOptions>(configuration.GetSection("Entries").GetSection("Storage").GetSection("FileSystem"))
            .AddSingleton(ImageFormatDefinition.Jpeg);
    }

    private static async Task DeleteExistingSampleContentAsync(EntriesDataContext entriesDb, IReadOnlyCollection<string> userIds, string repositoryRoot)
    {
        if (userIds.Count == 0)
            return;

        var entries = await entriesDb.Entries
            .Where(e => userIds.Contains(e.UserId))
            .ToListAsync();
        var entryIds = entries.Select(e => e.Id).ToArray();

        var stories = await entriesDb.Stories
            .Where(s => userIds.Contains(s.UserId))
            .Include(s => s.Chapters)
            .ToListAsync();
        var storyIds = stories.Select(s => s.Id).ToArray();

        var beings = await entriesDb.Beings
            .Where(b => userIds.Contains(b.UserId))
            .ToListAsync();
        var beingIds = beings.Select(b => b.Id).ToArray();

        entriesDb.EntryShares.RemoveRange(await entriesDb.EntryShares
            .Where(s => userIds.Contains(s.UserId) || entryIds.Contains(s.EntryId))
            .ToListAsync());
        entriesDb.StoryShares.RemoveRange(await entriesDb.StoryShares
            .Where(s => userIds.Contains(s.UserId) || storyIds.Contains(s.StoryId))
            .ToListAsync());
        entriesDb.BeingShares.RemoveRange(await entriesDb.BeingShares
            .Where(s => userIds.Contains(s.UserId) || beingIds.Contains(s.BeingId))
            .ToListAsync());

        entriesDb.Images.RemoveRange(await entriesDb.Images
            .Where(i => entryIds.Contains(i.Entry.Id))
            .ToListAsync());
        entriesDb.Videos.RemoveRange(await entriesDb.Videos
            .Where(v => entryIds.Contains(v.Entry.Id))
            .ToListAsync());

        entriesDb.RemoveRange(entries);
        entriesDb.RemoveRange(stories.SelectMany(s => s.Chapters));
        entriesDb.RemoveRange(stories);
        entriesDb.RemoveRange(beings);

        await entriesDb.SaveChangesAsync();

        string mediaRoot = Path.Combine(repositoryRoot, "artifacts", "media");
        foreach (string userId in userIds)
        {
            string userMediaPath = Path.Combine(mediaRoot, userId);
            if (Directory.Exists(userMediaPath))
                Directory.Delete(userMediaPath, recursive: true);
        }
    }

    private static async Task DeleteExistingAccountsStateAsync(AccountsDataContext accountsDb, UserManager<User> userManager, IReadOnlyCollection<User> users)
    {
        if (users.Count == 0)
            return;

        string[] userIds = users.Select(u => u.Id).ToArray();

        accountsDb.Connections.RemoveRange(await accountsDb.Connections
            .Where(c => userIds.Contains(c.UserId) || userIds.Contains(c.OtherUserId))
            .ToListAsync());
        accountsDb.UserProperties.RemoveRange(await accountsDb.UserProperties
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync());
        await accountsDb.SaveChangesAsync();

        foreach (User user in users)
        {
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to delete existing sample user '{user.UserName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    private static async Task<Dictionary<string, User>> CreateUsersAsync(UserManager<User> userManager)
    {
        var result = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        foreach (SampleUserDefinition definition in Users)
        {
            var user = new User(definition.UserName)
            {
                Email = definition.Email,
                EmailConfirmed = definition.IsPremium
            };

            var createResult = await userManager.CreateAsync(user, DefaultPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Unable to create sample user '{definition.UserName}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

            result.Add(definition.UserName, user);
        }

        return result;
    }

    private static async Task EnsureConnectionAsync(AccountsDataContext accountsDb, string firstUserId, string secondUserId)
    {
        accountsDb.Connections.Add(new UserConnection
        {
            UserId = firstUserId,
            OtherUserId = secondUserId,
            Permission = (int)Permission.Read,
            OtherPermission = (int)Permission.Read,
            State = 2
        });

        await accountsDb.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Being>> SeedBeingsAsync(EntriesDataContext entriesDb, IReadOnlyDictionary<string, User> users)
    {
        var beingsByName = new Dictionary<string, Being>(StringComparer.OrdinalIgnoreCase);

        foreach (SampleBeingDefinition definition in Beings)
        {
            User owner = users[definition.OwnerUserName];
            var being = new Being
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = owner.Id,
                Name = definition.Name,
                Icon = definition.Icon,
                Text = definition.Text,
                Created = DateTime.Now,
                IsSharingInherited = true
            };

            entriesDb.Beings.Add(being);
            beingsByName[definition.Name] = being;
        }

        await entriesDb.SaveChangesAsync();
        return beingsByName;
    }

    private static async Task SeedStoriesAsync(EntriesDataContext entriesDb, ImageService imageService, IReadOnlyDictionary<string, User> users, IReadOnlyList<string> mediaFiles, IReadOnlyDictionary<string, Being> beingsByName)
    {
        var seededEntries = new List<SeededEntry>();

        foreach (SampleStoryDefinition definition in Stories)
        {
            User owner = users[definition.OwnerUserName];
            var story = new Story
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = owner.Id,
                Title = definition.Title,
                Text = definition.Text,
                Created = definition.Entries.Min(e => e.When),
                IsSharingInherited = definition.IsSharingInherited
            };

            entriesDb.Stories.Add(story);

            foreach (string sharedWithUserName in definition.SharedWithUserNames)
            {
                entriesDb.StoryShares.Add(new StoryShare(story.Id, users[sharedWithUserName].Id)
                {
                    Permission = (int)Permission.Read
                });
            }

            foreach (SampleEntryDefinition entryDefinition in definition.Entries)
            {
                var entry = new Entry
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserId = owner.Id,
                    Title = entryDefinition.Title,
                    Text = entryDefinition.Text,
                    Story = story,
                    When = entryDefinition.When,
                    Created = entryDefinition.When.AddMinutes(30),
                    IsSharingInherited = true
                };

                for (int i = 0; i < entryDefinition.Locations.Length; i++)
                {
                    var location = entryDefinition.Locations[i];
                    entry.Locations.Add(new OrderedLocation
                    {
                        Order = i + 1,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Altitude = location.Altitude
                    });
                }

                entriesDb.Entries.Add(entry);
                seededEntries.Add(new SeededEntry(entry, entryDefinition.MediaCount));

                foreach (string beingName in entryDefinition.BeingNames)
                {
                    if (beingsByName.TryGetValue(beingName, out Being? being))
                        entry.Beings.Add(being);
                }
            }
        }

        await entriesDb.SaveChangesAsync();

        int mediaIndex = 0;
        foreach (SeededEntry seededEntry in seededEntries.Where(e => e.MediaCount > 0))
        {
            for (int i = 0; i < seededEntry.MediaCount; i++)
            {
                string mediaFile = mediaFiles[mediaIndex % mediaFiles.Count];
                mediaIndex++;

                await imageService.CreateAsync(seededEntry.Entry, new PhysicalFileInput(mediaFile));
            }
        }
    }

    private static IReadOnlyList<string> GetMediaFiles(string mediaRoot, string repositoryRoot)
    {
        Directory.CreateDirectory(mediaRoot);

        var files = Directory.EnumerateFiles(mediaRoot, "*.*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedMedia)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count != 0)
            return files;

        return
        [
            Path.Combine(repositoryRoot, "src", "Recollections.Blazor.UI", "wwwroot", "img", "background.jpg"),
            Path.Combine(repositoryRoot, "src", "Recollections.Blazor.UI", "wwwroot", "img", "icon-512x512.png"),
            Path.Combine(repositoryRoot, "src", "Recollections.Blazor.UI", "wwwroot", "img", "icon-192x192.png")
        ];
    }

    private static bool IsSupportedMedia(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(Directory.GetCurrentDirectory()); current != null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "src", "Recollections.Api", "appsettings.Development.json")))
                return current.FullName;
        }

        throw new InvalidOperationException("Unable to locate the repository root.");
    }

    private sealed record SampleUserDefinition(string UserName, string Email, bool IsPremium);

    private sealed record SampleStoryDefinition(
        string OwnerUserName,
        string Title,
        string Text,
        bool IsSharingInherited,
        string[] SharedWithUserNames,
        SampleEntryDefinition[] Entries
    );

    private sealed record SampleEntryDefinition(
        string Title,
        DateTime When,
        string Text,
        int MediaCount,
        SampleLocationDefinition[] Locations,
        string[]? BeingNames = null
    )
    {
        public string[] BeingNames { get; init; } = BeingNames ?? [];
    }

    private sealed record SampleLocationDefinition(double Latitude, double Longitude, double? Altitude = null);

    private sealed record SampleBeingDefinition(string OwnerUserName, string Name, string Icon, string Text);

    private sealed record SeededEntry(Entry Entry, int MediaCount);

    private sealed record SeedOptions
    {
        public string MediaDirectory { get; init; } = Path.Combine("assets", "sample-data", "media");

        public static SeedOptions Parse(string[] args)
        {
            var options = new SeedOptions();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--media", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("Missing value for '--media'.");

                    options = options with { MediaDirectory = args[++i] };
                }
                else
                {
                    throw new ArgumentException($"Unknown argument '{args[i]}'. Supported arguments: --media <path>.");
                }
            }

            return options;
        }

        public string ResolveMediaDirectory(string repositoryRoot)
            => Path.IsPathRooted(MediaDirectory)
                ? MediaDirectory
                : Path.GetFullPath(Path.Combine(repositoryRoot, MediaDirectory));
    }

    private sealed class PhysicalFileInput(string filePath) : IFileInput
    {
        public string ContentType => "image/" + Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        public string FileName => Path.GetFileName(filePath);

        public long Length => new FileInfo(filePath).Length;

        public Stream OpenReadStream() => File.OpenRead(filePath);
    }
}
