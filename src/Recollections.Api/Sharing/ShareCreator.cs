using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using DataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Sharing;

public class ShareCreator
{
    private readonly DataContext db;
    private readonly IUserNameProvider userNames;
    private readonly IConnectionProvider connections;

    public ShareCreator(DataContext db, IUserNameProvider userNames, IConnectionProvider connections)
    {
        Ensure.NotNull(db, "db");
        Ensure.NotNull(userNames, "userNames");
        Ensure.NotNull(connections, "connections");
        this.db = db;
        this.userNames = userNames;
        this.connections = connections;
    }

    private async Task<bool> SaveAsync<TEntity, TShare>(TEntity entity, Func<string, IQueryable<TShare>> findQuery, Func<string, TShare> entityFactory, ShareRootModel model)
        where TEntity : class, ISharingInherited
        where TShare : ShareBase
    {
        async Task SaveSingleAsync(ShareModel model, string userId)
        {
            var entity = await findQuery(userId).FirstOrDefaultAsync();

            if (model.Permission == null)
            {
                if (entity != null)
                    db.Set<TShare>().Remove(entity);
            }
            else
            {
                if (entity == null)
                    db.Set<TShare>().Add(entity = entityFactory(userId));

                entity.Permission = (int)model.Permission;
            }
        }

        var publicShare = model.Models.FirstOrDefault(s => s.UserName == ShareStatusService.PublicUserName);
        if (publicShare != null)
        {
            if (publicShare.Permission != null && publicShare.Permission != Permission.Read)
                return false;

            model.Models.Remove(publicShare);

            await SaveSingleAsync(publicShare, ShareStatusService.PublicUserId);
        }

        var userIds = await userNames.GetUserIdsAsync(model.Models.Select(m => m.UserName).ToArray());
        for (int i = 0; i < model.Models.Count; i++)
            await SaveSingleAsync(model.Models[i], userIds[i]);

        entity.IsSharingInherited = model.IsInherited;
        db.Set<TEntity>().Update(entity);

        await db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HasPermissionAsync(string userId, ShareRootModel model, Permission sharePermission)
    {
        var storyOwnerId = await userNames.GetUserNameAsync(userId);
        var storyOwnerShare = model.Models.FirstOrDefault(s => s.UserName == storyOwnerId);
        return storyOwnerShare == null
            || (sharePermission == Permission.CoOwner && storyOwnerShare.Permission == Permission.CoOwner)
            || (sharePermission == Permission.Read && storyOwnerShare.Permission != null);
    }

    public async Task<bool> SaveEntryAsync(Entry entry, ShareRootModel model)
    {
        // Ensure story owner has co-owner permission to entry.
        if (entry.Story != null && entry.UserId != entry.Story.UserId)
        {
            if (model.IsInherited)
            {
                var permission = (Permission?)await connections.GetPermissionAsync(entry.UserId, entry.Story.UserId);
                if (permission != Permission.CoOwner)
                    return false;
            }
            else
            {
                if (!await HasPermissionAsync(entry.Story.UserId, model, Permission.CoOwner))
                    return false;
            }
        }

        // Ensure being owner has co-owner permission to entry.
        foreach (var being in entry.Beings)
        {
            if (entry.UserId != being.UserId)
            {
                if (!await HasPermissionAsync(being.UserId, model, Permission.CoOwner))
                    return false;
            }
        }

        return await SaveAsync(
            entry,
            userId => db.EntryShares.Where(s => s.EntryId == entry.Id && s.UserId == userId),
            userId => new EntryShare(entry.Id, userId),
            model
        );
    }

    public async Task<bool> SaveStoryAsync(Story story, ShareRootModel model)
    {
        // Ensure entry owner has co-owner permission to story.
        foreach (var entry in db.Entries.Where(e => (e.Story.Id == story.Id || e.Chapter.Story.Id == story.Id) && e.UserId != story.UserId))
        {
            if (entry.IsSharingInherited)
            {
                var permission = (Permission?)await connections.GetPermissionAsync(entry.UserId, story.UserId);
                if (permission != Permission.CoOwner)
                    return false;
            }
            else
            {
                if (!await HasPermissionAsync(entry.UserId, model, Permission.CoOwner))
                    return false;
            }
        }

        return await SaveAsync(
            story,
            userId => db.StoryShares.Where(s => s.StoryId == story.Id && s.UserId == userId),
            userId => new StoryShare(story.Id, userId),
            model
        );
    }

    public async Task<bool> SaveBeingAsync(Being being, ShareRootModel model)
    {
        foreach (var item in model.Models)
        {
            if (being.Id == being.UserId && !(item.UserName == null || item.UserName == ShareStatusService.PublicUserName))
                return false;
        }

        // Ensure entry owner has co-owner permission to being.
        foreach (var entry in db.Entries.Where(e => e.Beings.Any(b => b.Id == being.Id) && e.UserId != being.UserId))
        {
            if (entry.IsSharingInherited)
            {
                var permission = (Permission?)await connections.GetPermissionAsync(entry.UserId, being.UserId);
                if (permission != Permission.CoOwner)
                    return false;
            }
            else
            {
                if (!await HasPermissionAsync(entry.UserId, model, Permission.Read))
                    return false;
            }
        }

        return await SaveAsync(
            being,
            userId => db.BeingShares.Where(s => s.BeingId == being.Id && s.UserId == userId),
            userId => new BeingShare(being.Id, userId),
            model
        );
    }

    public async Task<bool> CreateBeingAsync(string beingId, string userName, Permission permission)
        => await CreateBeingAsync(await db.Beings.SingleAsync(b => b.Id == beingId), new ShareModel(userName, permission));

    public Task<bool> CreateBeingAsync(Being being, ShareModel model)
    {
        return CreateAsync(
            model,
            userId => db.BeingShares.Where(s => s.BeingId == being.Id && s.UserId == userId),
            () => new BeingShare(being.Id)
        );
    }

    private async Task<bool> CreateAsync<T>(ShareModel model, Func<string, IQueryable<T>> findQuery, Func<T> entityFactory)
        where T : ShareBase
    {
        string userId;
        string userName = model.UserName;
        if (userName != null)
        {
            userName = userName.Trim();
            if (userName != ShareStatusService.PublicUserName)
                userId = (await userNames.GetUserIdsAsync(new[] { userName })).First();
            else
                userId = ShareStatusService.PublicUserId;
        }
        else
        {
            userId = ShareStatusService.PublicUserId;
        }

        if (userId == ShareStatusService.PublicUserId && model.Permission != Permission.Read)
            return false;

        T entity = await findQuery(userId).FirstOrDefaultAsync();
        if (entity == null)
        {
            entity = entityFactory();
            entity.UserId = userId;
            entity.Permission = (int)model.Permission;

            await db.Set<T>().AddAsync(entity);
        }
        else
        {
            entity.Permission = (int)model.Permission;
        }

        await db.SaveChangesAsync();
        return true;
    }
}