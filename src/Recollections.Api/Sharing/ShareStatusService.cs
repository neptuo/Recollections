using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Sharing
{
    public class ShareStatusService
    {
        public const string PublicUserId = "public";
        public const string PublicUserName = "public";

        private readonly DataContext db;
        private readonly IConnectionProvider connections;

        public ShareStatusService(DataContext db, IConnectionProvider connections)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.connections = connections;
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

        private async Task<Permission?> GetPermissionAsync<TEntity, TShare>(TEntity entity, IQueryable<TShare> findShareQuery, string userId) 
            where TEntity : IOwnerByUser, ISharingInherited
            where TShare: ShareBase
        {
            if (entity.UserId == userId)
                return Permission.CoOwner;
            
            var connectionPermission = (Permission?)await connections.GetPermissionAsync(userId, entity.UserId);
            if (connectionPermission == null)
                return null;

            var permissions = await findShareQuery.Where(s => s.UserId == userId || s.UserId == PublicUserId).Select(s => s.Permission).ToListAsync();
            if (permissions.Count != 0)
                return (Permission)permissions.Max();

            return connectionPermission;
        }

        public Task<Permission?> GetEntryPermissionAsync(Entry entry, string userId)
        {
            IQueryable<ShareBase> findShareQuery = null;
            if (entry.IsSharingInherited)
                findShareQuery = db.StoryShares.Where(s => s.StoryId == db.Entries.Single(e => e.Id == entry.Id).Story.Id || s.StoryId == db.Entries.Single(e => e.Id == entry.Id).Chapter.Story.Id);
            else
                findShareQuery = db.EntryShares.Where(s => s.EntryId == entry.Id);

            return GetPermissionAsync(entry, findShareQuery, userId);
        }

        public IQueryable<Entry> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Entry> query, string userId, IEnumerable<string> connectionReadUserIds)
        {
            if (userId == null)
                userId = PublicUserId;

            // IReadOnlyList<string> readerUserIds = await connectionProvider.GetUserIdsWithReaderToAsync(userId);
            // return query.Where(e => e.UserId == userId || db.EntryShares.Any(s => s.EntryId == e.Id && s.UserId == userId) || (e.IsSharingInherited && readerUserIds.Contains(e.UserId)));
            return query.Where(
                e => e.UserId == userId || (
                    connectionReadUserIds.Contains(e.UserId) && (
                        db.EntryShares.Any(s => s.EntryId == e.Id && s.UserId == userId)
                        || (e.IsSharingInherited
                            && (db.StoryShares.Any(s => s.StoryId == e.Story.Id && s.UserId == userId)
                                || db.StoryShares.Any(s => s.StoryId == e.Chapter.Story.Id && s.UserId == userId)
                                || connectionReadUserIds.Contains(e.UserId)
                            )
                        )
                    )
                )
            );
        }

        public async Task<bool> IsStorySharedForReadAsync(string storyId, string userId)
        {
            bool isAllowed = await db.StoryShares.AnyAsync(s => s.StoryId == storyId && (s.UserId == userId || s.UserId == PublicUserId));
            return isAllowed;
        }

        public Task<Permission?> GetStoryPermissionAsync(Story story, string userId)
            => GetPermissionAsync(story, db.StoryShares.Where(s => s.StoryId == story.Id), userId);

        public async Task<bool> IsStorySharedForWriteAsync(string storyId, string userId)
        {
            bool isAllowed = await db.StoryShares.AnyAsync(s => s.StoryId == storyId && (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.CoOwner);
            return isAllowed;
        }

        public IQueryable<Story> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Story> query, string userId, IEnumerable<string> connectionReadUserIds)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(
                e => e.UserId == userId || (
                    connectionReadUserIds.Contains(e.UserId) && (
                        db.StoryShares.Any(s => s.StoryId == e.Id && s.UserId == userId) 
                        || (e.IsSharingInherited && connectionReadUserIds.Contains(e.UserId))
                    )
                )
            );
        }

        public Task<Permission?> GetBeingPermissionAsync(Being being, string userId)
            => GetPermissionAsync(being, db.BeingShares.Where(s => s.BeingId == being.Id), userId);

        public IQueryable<Being> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Being> query, string userId, IEnumerable<string> connectionReadUserIds)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(
                e => e.UserId == userId || (
                    connectionReadUserIds.Contains(e.UserId) && (
                        db.BeingShares.Any(s => s.BeingId == e.Id && s.UserId == userId) 
                        || (e.IsSharingInherited && connectionReadUserIds.Contains(e.UserId))
                    )
                )
            );
        }
    }
}
