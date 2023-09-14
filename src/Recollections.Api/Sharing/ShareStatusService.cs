using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> IsSharedForReadAsync<T>(IQueryable<T> findQuery, string userId)
            where T : ShareBase
        {
            bool isAllowed = await findQuery.AnyAsync(s => s.UserId == userId || s.UserId == PublicUserId);
            return isAllowed;
        }

        public async Task<bool> IsSharedAsCoOwnerAsync<T>(IQueryable<T> findQuery, string userId)
            where T : ShareBase
        {
            bool isAllowed = await findQuery.AnyAsync(s => (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.CoOwner);
            return isAllowed;
        }

        public async Task<Permission?> GetPermissionAsync<TEntity, TShare>(TEntity entity, IQueryable<TShare> findQuery, string userId) 
            where TEntity : IOwnerByUser
            where TShare: ShareBase
        {
            if (entity.UserId == userId)
                return Permission.CoOwner;
            
            var permissions = await findQuery.Where(s => s.UserId == userId || s.UserId == PublicUserId).Select(s => s.Permission).ToListAsync();
            if (permissions.Count == 0)
                return null;
            
            return (Permission)permissions.Max();
        }

        public Task<bool> IsEntrySharedForReadAsync(string entryId, string userId)
            => IsSharedForReadAsync(db.EntryShares.Where(s => s.EntryId == entryId), userId);

        public Task<bool> IsEntrySharedAsCoOwnerAsync(string entryId, string userId)
            => IsSharedAsCoOwnerAsync(db.EntryShares.Where(s => s.EntryId == entryId), userId);

        public Task<Permission?> GetEntryPermissionAsync(Entry entry, string userId)
            => GetPermissionAsync(entry, db.EntryShares.Where(s => s.EntryId == entry.Id), userId);

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
            bool isAllowed = await db.StoryShares.AnyAsync(s => s.StoryId == storyId && (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.CoOwner);
            return isAllowed;
        }

        public IQueryable<Story> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Story> query, string userId)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(e => e.UserId == userId || db.StoryShares.Any(s => s.StoryId == e.Id && s.UserId == userId));
        }

        public async Task<bool> IsBeingSharedForReadAsync(string beingId, string userId)
        {
            bool isAllowed = await db.BeingShares.AnyAsync(s => s.BeingId == beingId && (s.UserId == userId || s.UserId == PublicUserId));
            return isAllowed;
        }

        public async Task<bool> IsBeingSharedForWriteAsync(string beingId, string userId)
        {
            bool isAllowed = await db.BeingShares.AnyAsync(s => s.BeingId == beingId && (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.CoOwner);
            return isAllowed;
        }

        public IQueryable<Being> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Being> query, string userId)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(e => e.UserId == userId || db.BeingShares.Any(s => s.BeingId == e.Id && s.UserId == userId));
        }

        public async Task<bool> IsProfileSharedForReadAsync(string profileId, string userId)
        {
            bool isAllowed = await db.ProfileShares.AnyAsync(s => s.ProfileId == profileId && (s.UserId == userId || s.UserId == PublicUserId));
            return isAllowed;
        }
    }
}
