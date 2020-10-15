using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class ShareStatusService
    {
        public const string PublicUserId = "public";
        public const string PublicUserName = "public";

        private readonly DataContext db;

        public ShareStatusService(DataContext db)
        {
            Ensure.NotNull(db, "db");
            this.db = db;
        }

        public async Task<bool> IsEntrySharedForReadAsync(string entryId, string userId)
        {
            bool isAllowed = await db.EntryShares.AnyAsync(s => s.EntryId == entryId && (s.UserId == userId || s.UserId == PublicUserId));
            return isAllowed;
        }

        public async Task<bool> IsEntrySharedForWriteAsync(string entryId, string userId)
        {
            bool isAllowed = await db.EntryShares.AnyAsync(s => s.EntryId == entryId && (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.Write);
            return isAllowed;
        }

        public IQueryable<Entry> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Entry> query, string userId)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(e => e.UserId == userId || db.EntryShares.Any(s => s.EntryId == e.Id && s.UserId == userId));
        }

        public async Task<bool> IsStorySharedForReadAsync(string storyId, string userId)
        {
            bool isAllowed = await db.StoryShares.AnyAsync(s => s.StoryId == storyId && (s.UserId == userId || s.UserId == PublicUserId));
            return isAllowed;
        }

        public async Task<bool> IsStorySharedForWriteAsync(string storyId, string userId)
        {
            bool isAllowed = await db.StoryShares.AnyAsync(s => s.StoryId == storyId && (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.Write);
            return isAllowed;
        }

        public IQueryable<Story> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Story> query, string userId)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(e => e.UserId == userId || db.StoryShares.Any(s => s.StoryId == e.Id && s.UserId == userId));
        }
    }
}
