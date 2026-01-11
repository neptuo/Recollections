using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;

namespace Neptuo.Recollections.Entries;

public class EntryListMapper(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus)
{
    private const int PageSize = 10;
    private IUserNameProvider userNames = userNames;

    public Task<(List<EntryListModel> models, bool hasMore)> MapAsync(IQueryable<Entry> query, string userId, ConnectedUsersModel connectedUsers, int offset)
        => MapAsync(query, userId, connectedUsers, offset, PageSize);

    public async Task<(List<EntryListModel> models, bool hasMore)> MapAsync(IQueryable<Entry> query, string userId, ConnectedUsersModel connectedUsers, int? offset = null, int? pageSize = null)
    {
        if (offset != null)
        {
            Ensure.PositiveOrZero(offset.Value, "offset");
            query = query.Skip(offset.Value);
        }

        if (pageSize != null)
        {
            Ensure.Positive(pageSize.Value, "pageSize");
            query = query.Take(pageSize.Value);
        }

        var result = await query
            .Select(e => new
            {
                UserId = e.UserId,
                Id = e.Id,
                Title = e.Title,
                When = e.When,
                StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                ChapterTitle = e.Chapter.Title,
                Beings = new List<EntryBeingModel>(),
                ImageCount = dataContext.Images.Count(i => i.Entry.Id == e.Id),
                VideoCount = dataContext.Videos.Count(v => v.Entry.Id == e.Id),
                GpsCount = e.Locations.Count,
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
                entry.Beings.AddRange(await shareStatus
                    .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
                    .Where(e => e.Id == entry.Id)
                    .SelectMany(e => e.Beings)
                    .OrderBy(b => b.Name)
                    .Select(b => new EntryBeingModel(
                        Id: b.Id,
                        Name: b.Name,
                        Icon: b.Icon
                    ))
                    .ToListAsync()
                );
            }
        }

        var userNames = await this.userNames.GetUserNamesAsync(result.Select(e => e.UserId).ToArray());
        return (result.Select((e, index) => new EntryListModel(
            UserId: e.UserId,
            UserName: userNames[index],
            Id: e.Id,
            Title: e.Title,
            TextWordCount: (e.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            When: e.When,
            StoryTitle: e.StoryTitle,
            ChapterTitle: e.ChapterTitle,
            Beings: e.Beings,
            ImageCount: e.ImageCount,
            VideoCount: e.VideoCount,
            GpsCount: e.GpsCount
        )).ToList(), result.Count == pageSize);
    }
}