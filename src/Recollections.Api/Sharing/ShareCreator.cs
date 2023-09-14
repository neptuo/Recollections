using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

    public ShareCreator(DataContext db, IUserNameProvider userNames)
    {
        Ensure.NotNull(db, "db");
        Ensure.NotNull(userNames, "userNames");
        this.db = db;
        this.userNames = userNames;
    }

    private async Task<bool> SaveAsync<T>(Func<string, IQueryable<T>> findQuery, Func<string, T> entityFactory, List<ShareModel> models)
        where T : ShareBase
    {
        async Task SaveSingleAsync(ShareModel model, string userId)
        {
            var entity = await findQuery(userId).FirstOrDefaultAsync();
            
            if (model.Permission == null)
            {
                if (entity != null)
                    db.Set<T>().Remove(entity);
            }
            else 
            {
                if (entity == null)
                    db.Set<T>().Add(entity = entityFactory(userId));

                entity.Permission = (int)model.Permission;
            }
        }

        var publicShare = models.FirstOrDefault(s => s.UserName == ShareStatusService.PublicUserName);
        if (publicShare != null)
        {
            if (publicShare.Permission != null && publicShare.Permission != Permission.Read)
                return false;

            models.Remove(publicShare);

            await SaveSingleAsync(publicShare, ShareStatusService.PublicUserId);
        }

        var userIds = await userNames.GetUserIdsAsync(models.Select(m => m.UserName).ToArray());
        for (int i = 0; i < models.Count; i++)
            await SaveSingleAsync(models[i], userIds[i]);

        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> SaveEntryAsync(Entry entry, List<ShareModel> models) 
    {
        return SaveAsync(
            userId => db.EntryShares.Where(s => s.EntryId == entry.Id && s.UserId == userId),
            userId => new EntryShare(entry.Id, userId),
            models
        );
    }

    public Task<bool> SaveStoryAsync(Story story, List<ShareModel> models) 
    {
        return SaveAsync(
            userId => db.StoryShares.Where(s => s.StoryId == story.Id && s.UserId == userId),
            userId => new StoryShare(story.Id, userId),
            models
        );
    }

    public Task<bool> SaveBeingAsync(Being being, List<ShareModel> models) 
    {   
        foreach (var model in models)
        {
            if (being.Id == being.UserId && model.UserName != null)
                return Task.FromResult(false);
        }

        return SaveAsync(
            userId => db.BeingShares.Where(s => s.BeingId == being.Id && s.UserId == userId),
            userId => new BeingShare(being.Id, userId),
            models
        );
    }

    public Task<bool> CreateEntryAsync(Entry entry, ShareModel model) 
    {
        return CreateAsync(
            model, 
            userId => db.EntryShares.Where(s => s.EntryId == entry.Id && s.UserId == userId), 
            () => new EntryShare(entry.Id)
        );
    }

    public Task<bool> CreateStoryAsync(Story story, ShareModel model) 
    {
        return CreateAsync(
            model, 
            userId => db.StoryShares.Where(s => s.StoryId == story.Id && s.UserId == userId), 
            () => new StoryShare(story.Id)
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