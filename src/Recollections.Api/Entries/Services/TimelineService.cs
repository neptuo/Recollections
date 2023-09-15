using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;

namespace Neptuo.Recollections.Entries;

public class TimelineService
{
    private const int PageSize = 10;
    
    private readonly DataContext dataContext;
    private readonly IUserNameProvider userNames;
    private readonly ShareStatusService shareStatus;

    public TimelineService(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus)
    {
        Ensure.NotNull(dataContext, "dataContext");
        Ensure.NotNull(userNames, "userNames");
        Ensure.NotNull(shareStatus, "shareStatus");
        this.dataContext = dataContext;
        this.userNames = userNames;
        this.shareStatus = shareStatus;
    }

    public async Task<(List<TimelineEntryModel> models, bool hasMore)> GetAsync(IQueryable<Entry> query, string userId, IEnumerable<string> connectionReadUserIds, int offset)
    {
        Ensure.NotNullOrEmpty(userId, "userId");
        Ensure.PositiveOrZero(offset, "offset");

        var result = await query
            .OrderByDescending(e => e.When)
            .Skip(offset)
            .Take(PageSize)
            .Select(e => new
            {
                Model = new TimelineEntryModel()
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    Title = e.Title,
                    When = e.When,
                    StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                    ChapterTitle = e.Chapter.Title,
                    GpsCount = e.Locations.Count,
                    ImageCount = dataContext.Images.Count(i => i.Entry.Id == e.Id),
                    //Beings = e.Beings
                    //    .Select(b => new TimelineEntryBeingModel()
                    //    {
                    //        Name = b.Name,
                    //        Icon = b.Icon
                    //    }).ToList()
                },
                BeingCount = e.Beings.Count(),
                Text = e.Text
            })
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        foreach (var entry in result)
        {
            if (entry.BeingCount > 0) 
            {
                entry.Model.Beings = await shareStatus
                    .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectionReadUserIds)
                    .Where(e => e.Id == entry.Model.Id)
                    .SelectMany(e => e.Beings)
                    .OrderBy(b => b.Name)
                    .Select(b => new TimelineEntryBeingModel()
                    {
                        Name = b.Name,
                        Icon = b.Icon
                    })
                    .ToListAsync();
            }

            if (!String.IsNullOrEmpty(entry.Text))
                entry.Model.TextWordCount = entry.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        var userNames = await this.userNames.GetUserNamesAsync(result.Select(e => e.Model.UserId).ToArray());
        for (int i = 0; i < result.Count; i++)
            result[i].Model.UserName = userNames[i];

        return (result.Select(e => e.Model).ToList(), result.Count == PageSize);
    }
}