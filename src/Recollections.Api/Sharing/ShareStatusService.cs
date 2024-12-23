﻿using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        private async Task<Permission?> GetPermissionAsync<TEntity, TShare>(TEntity entity, IQueryable<TShare> findShareQuery, string userId, bool? isSharingInherited = null) 
            where TEntity : IOwnerByUser, ISharingInherited
            where TShare: ShareBase
        {
            if (entity.UserId == userId)
                return Permission.CoOwner;

            // If inheriting is enabled, we don't care about saved permissions
            if (isSharingInherited ?? entity.IsSharingInherited)
            {
                // If user is authenticated, check connection permission
                if (userId != PublicUserId)
                {
                    var connectionPermission = (Permission?)await connections.GetPermissionAsync(userId, entity.UserId);
                    if (connectionPermission != null)
                        return connectionPermission.Value;
                }

                return null;
            }

            // Find shares for user and public user
            var shares = await findShareQuery
                .Where(s => s.UserId == userId || s.UserId == PublicUserId)
                .Select(s => new { s.UserId, s.Permission })
                .ToListAsync();

            // Check if exists share for user
            var userPermission = (Permission?)shares.FirstOrDefault(s => s.UserId == userId)?.Permission;
            if (userPermission != null)
                return userPermission.Value;

            // Check if exists share for public
            var publicPermission = (Permission?)shares.FirstOrDefault(s => s.UserId == PublicUserId)?.Permission;
            if (publicPermission != null)
                return publicPermission.Value;

            // No access
            return null;
        }

        public async Task<Permission?> GetEntryPermissionAsync(Entry entry, string userId)
        {
            if (entry.UserId == userId)
                return Permission.CoOwner;

            IQueryable<ShareBase> findShareQuery = null;
            bool isSharingInherited = entry.IsSharingInherited;
            if (entry.IsSharingInherited)
            {
                // If entry inherits sharing, we need look for shares and whether story inherits sharing.
                var story = await db.Entries
                    .Where(e => e.Id == entry.Id)
                    .Where(e => e.Story != null || e.Chapter != null)
                    .Select(e => new 
                    { 
                        StoryId = e.Story.Id, 
                        ChapterId = e.Chapter.Id, 
                        IsSharingInherited = (e.Story != null ? e.Story.IsSharingInherited : false) || (e.Chapter != null ? e.Chapter.Story.IsSharingInherited : false)
                    })
                    .SingleOrDefaultAsync();

                if (story != null && (story.StoryId != null || story.ChapterId != null))
                {
                    isSharingInherited = story.IsSharingInherited;
                }
                else
                {
                    // Look if there is a non-user being that a) I own, b) someone share with me
                    var share = await db.BeingShares
                        .Where(ds => db.Entries
                            .Where(e => e.Id == entry.Id)
                            .SelectMany(e => e.Beings)
                            .Where(b => b.UserId == userId // Owned beings tagged
                                || (b.Id != b.UserId && !b.IsSharingInherited && db.BeingShares.Any(s => s.BeingId == b.Id && s.UserId == userId)) // Non-user shared being attached
                            )
                            .Select(b => b.Id)
                            .Contains(ds.BeingId)
                        )
                        .OrderByDescending(ds => ds.Permission)
                        .FirstOrDefaultAsync();

                    if (share != null)
                        return (Permission)share.Permission;
                }

                findShareQuery = db.StoryShares.Where(s => s.StoryId == db.Entries.Single(e => e.Id == entry.Id).Story.Id || s.StoryId == db.Entries.Single(e => e.Id == entry.Id).Chapter.Story.Id);
            }
            else
            {
                findShareQuery = db.EntryShares.Where(s => s.EntryId == entry.Id);
            }

            return await GetPermissionAsync(entry, findShareQuery, userId, isSharingInherited: isSharingInherited);
        }

        public IQueryable<Entry> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Entry> query, string userId, ConnectedUsersModel connectedUsers)
            => OwnedByOrExplicitlySharedWithUser(db, query, [userId], connectedUsers);

        public IQueryable<Entry> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Entry> query, string[] userIds, ConnectedUsersModel connectedUsers)
            => query.Where(OwnedByOrExplicitlySharedWithUser(db, userIds, connectedUsers));

        private Expression<Func<Entry, bool>> OwnedByOrExplicitlySharedWithUser(DataContext db, string[] userIds, ConnectedUsersModel connectedUsers)
        {
            for (int i = 0; i < userIds.Length; i++)
            {
                if (userIds[i] == null)
                    userIds[i] = PublicUserId;
            }

            return e => userIds.Contains(e.UserId) || ( // Entry owner
                (connectedUsers.ActiveUserIds.Contains(e.UserId) || userIds.Contains(PublicUserId)) && (
                    (!e.IsSharingInherited && db.EntryShares.Any(s => s.EntryId == e.Id && userIds.Contains(s.UserId))) // Shared entry
                    || (e.IsSharingInherited // Entry inherits
                        && (
                            ((!e.Story.IsSharingInherited && db.StoryShares.Any(s => s.StoryId == e.Story.Id && userIds.Contains(s.UserId))) // Shared story
                                || (!e.Chapter.Story.IsSharingInherited && db.StoryShares.Any(s => s.StoryId == e.Chapter.Story.Id && userIds.Contains(s.UserId))) // Shared story through chapter
                                || ((e.Story.IsSharingInherited || e.Chapter.Story.IsSharingInherited || (e.Story == null && e.Chapter.Story == null)) // Story inherits or is null
                                    && connectedUsers.ReaderUserIds.Contains(e.UserId) // Shared connection
                                )
                            )
                            || (e.Beings
                                .Any(b => userIds.Contains(b.UserId) // Owned beings tagged
                                    || (b.Id != b.UserId && !b.IsSharingInherited && db.BeingShares.Any(s => s.BeingId == b.Id && userIds.Contains(s.UserId))) // Non-user shared being attached
                                )
                            )
                        )
                    )
                )
            );
        }

        public Task<Permission?> GetStoryPermissionAsync(Story story, string userId)
            => GetPermissionAsync(story, db.StoryShares.Where(s => s.StoryId == story.Id), userId);

        public IQueryable<Story> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Story> query, string userId, ConnectedUsersModel connectedUsers)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(
                e => e.UserId == userId || (
                    (connectedUsers.ActiveUserIds.Contains(e.UserId) || userId == PublicUserId) && (
                        (!e.IsSharingInherited && db.StoryShares.Any(s => s.StoryId == e.Id && s.UserId == userId))
                        || (e.IsSharingInherited && connectedUsers.ReaderUserIds.Contains(e.UserId))
                    )
                )
            );
        }

        public Task<Permission?> GetBeingPermissionAsync(Being being, string userId)
            => GetPermissionAsync(being, db.BeingShares.Where(s => s.BeingId == being.Id), userId);

        public IQueryable<Being> OwnedByOrExplicitlySharedWithUser(DataContext db, IQueryable<Being> query, string userId, ConnectedUsersModel connectedUsers)
        {
            if (userId == null)
                userId = PublicUserId;

            return query.Where(
                e => e.UserId == userId || (
                    (connectedUsers.ActiveUserIds.Contains(e.UserId) || userId == PublicUserId) && (
                        (!e.IsSharingInherited && db.BeingShares.Any(s => s.BeingId == e.Id && s.UserId == userId))
                        || (e.IsSharingInherited && connectedUsers.ReaderUserIds.Contains(e.UserId))
                    )
                )
            );
        }
    }
}
