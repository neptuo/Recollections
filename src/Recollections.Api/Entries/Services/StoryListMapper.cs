using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;

namespace Neptuo.Recollections.Entries;

public class StoryListMapper(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus)
{
    private const int PageSize = 10;
    private const int MaxPageSize = PageSize * 100;

    public Task<(List<StoryListModel> models, bool hasMore)> MapAsync(IQueryable<Story> query, string userId, ConnectedUsersModel connectedUsers, int offset)
        => MapAsync(query, userId, connectedUsers, offset, PageSize);

    public static int NormalizePageSize(int? pageSize)
    {
        int normalizedPageSize = pageSize ?? PageSize;
        Ensure.Positive(normalizedPageSize, "pageSize");
        return Math.Min(normalizedPageSize, MaxPageSize);
    }

    private async Task<List<StoryListModel>> MapListAsync(List<Story> stories, string userId, ConnectedUsersModel connectedUsers)
    {
        if (stories.Count == 0)
            return [];

        var storyIds = stories
            .Select(s => s.Id)
            .ToArray();

        var chapterCounts = await dataContext.Stories
            .Where(s => storyIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                Chapters = s.Chapters.Count
            })
            .ToDictionaryAsync(s => s.Id, s => s.Chapters);

        var entryStats = await shareStatus
            .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, [userId, ShareStatusService.PublicUserId], connectedUsers)
            .Where(e =>
                (e.Story != null && storyIds.Contains(e.Story.Id))
                || (e.Chapter != null && storyIds.Contains(e.Chapter.Story.Id))
            )
            .Select(e => new
            {
                StoryId = e.Story != null ? e.Story.Id : e.Chapter.Story.Id,
                e.When
            })
            .GroupBy(e => e.StoryId)
            .Select(g => new
            {
                StoryId = g.Key,
                Entries = g.Count(),
                MinDate = g.Min(e => e.When),
                MaxDate = g.Max(e => e.When)
            })
            .ToDictionaryAsync(s => s.StoryId);

        List<StoryListModel> models = new(stories.Count);
        foreach (var story in stories)
        {
            var model = new StoryListModel()
            {
                Id = story.Id,
                UserId = story.UserId,
                Title = story.Title
            };

            if (chapterCounts.TryGetValue(story.Id, out var chapters))
                model.Chapters = chapters;

            if (entryStats.TryGetValue(story.Id, out var entryStat))
            {
                model.Entries = entryStat.Entries;
                model.MinDate = entryStat.MinDate;
                model.MaxDate = entryStat.MaxDate;
            }

            models.Add(model);
        }

        var userNamesList = await userNames.GetUserNamesAsync(models.Select(e => e.UserId).ToArray());
        for (int i = 0; i < models.Count; i++)
            models[i].UserName = userNamesList[i];

        Sort(models);

        return models;
    }

    public async Task<(List<StoryListModel> models, bool hasMore)> MapAsync(IQueryable<Story> query, string userId, ConnectedUsersModel connectedUsers, int? offset = null, int? pageSize = null)
    {
        if (offset != null)
            Ensure.PositiveOrZero(offset.Value, "offset");

        int? normalizedPageSize = null;
        if (pageSize != null)
            normalizedPageSize = NormalizePageSize(pageSize.Value);

        var stories = await query.ToListAsync();
        var models = await MapListAsync(stories, userId, connectedUsers);

        if (offset != null)
            models = models.Skip(offset.Value).ToList();

        if (normalizedPageSize != null)
        {
            bool hasMore = models.Count > normalizedPageSize.Value;
            models = models.Take(normalizedPageSize.Value).ToList();
            return (models, hasMore);
        }

        return (models, false);
    }

    private static void Sort(List<StoryListModel> models)
    {
        models.Sort((a, b) =>
        {
            int compare = (b.MaxDate ?? DateTime.MinValue).CompareTo(a.MaxDate ?? DateTime.MinValue);
            if (compare == 0)
                compare = (b.MinDate ?? DateTime.MinValue).CompareTo(a.MinDate ?? DateTime.MinValue);

            if (compare == 0)
                compare = a.Title.CompareTo(b.Title);

            return compare;
        });
    }
}
