using System;
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

    public ShareCreator(DataContext db, IUserNameProvider userNames)
    {
        Ensure.NotNull(db, "db");
        Ensure.NotNull(userNames, "userNames");
        this.db = db;
        this.userNames = userNames;
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