using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Tests.Infrastructure;

public static class DatabaseSeeder
{
    /// <summary>
    /// Creates an Identity user in the Accounts database.
    /// Does NOT create the user's Being (profile) — use <see cref="SeedUserBeing"/> for that.
    /// </summary>
    public static async Task SeedUser(AccountsDataContext db, string userId, string userName)
    {
        db.Users.Add(new User(userName)
        {
            Id = userId,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@test.local",
            NormalizedEmail = $"{userName}@test.local".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates the user's Being (profile Being where Id == UserId).
    /// </summary>
    public static async Task SeedUserBeing(EntriesDataContext db, string userId, string userName)
    {
        db.Beings.Add(new Being
        {
            Id = userId,
            UserId = userId,
            Name = userName,
            Created = DateTime.UtcNow,
            IsSharingInherited = false,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a user connection between two users.
    /// permission1 = what user1 grants to user2.
    /// permission2 = what user2 grants to user1.
    /// </summary>
    public static async Task SeedConnection(AccountsDataContext db, string userId1, string userId2, Permission? permission1, Permission? permission2, int state = 2)
    {
        db.Connections.Add(new UserConnection
        {
            UserId = userId1,
            OtherUserId = userId2,
            Permission = (int?)permission1,
            OtherPermission = (int?)permission2,
            State = state,
        });
        await db.SaveChangesAsync();
    }

    public static async Task<Entry> SeedEntry(EntriesDataContext db, string entryId, string userId, bool isSharingInherited = true, Story story = null, StoryChapter chapter = null)
    {
        var entry = new Entry
        {
            Id = entryId,
            UserId = userId,
            Title = $"Entry {entryId}",
            When = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            IsSharingInherited = isSharingInherited,
            Story = story,
            Chapter = chapter,
        };
        db.Entries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public static async Task<Story> SeedStory(EntriesDataContext db, string storyId, string userId, bool isSharingInherited = true)
    {
        var story = new Story
        {
            Id = storyId,
            UserId = userId,
            Title = $"Story {storyId}",
            Created = DateTime.UtcNow,
            IsSharingInherited = isSharingInherited,
        };
        db.Stories.Add(story);
        await db.SaveChangesAsync();
        return story;
    }

    public static async Task<Being> SeedBeing(EntriesDataContext db, string beingId, string userId, bool isSharingInherited = false)
    {
        var being = new Being
        {
            Id = beingId,
            UserId = userId,
            Name = $"Being {beingId}",
            Created = DateTime.UtcNow,
            IsSharingInherited = isSharingInherited,
        };
        db.Beings.Add(being);
        await db.SaveChangesAsync();
        return being;
    }

    public static async Task SeedEntryShare(EntriesDataContext db, string entryId, string userId, Permission permission)
    {
        db.EntryShares.Add(new EntryShare(entryId, userId) { Permission = (int)permission });
        await db.SaveChangesAsync();
    }

    public static async Task SeedStoryShare(EntriesDataContext db, string storyId, string userId, Permission permission)
    {
        db.StoryShares.Add(new StoryShare(storyId, userId) { Permission = (int)permission });
        await db.SaveChangesAsync();
    }

    public static async Task SeedBeingShare(EntriesDataContext db, string beingId, string userId, Permission permission)
    {
        db.BeingShares.Add(new BeingShare(beingId, userId) { Permission = (int)permission });
        await db.SaveChangesAsync();
    }
}
