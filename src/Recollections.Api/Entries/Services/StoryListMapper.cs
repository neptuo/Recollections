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
    public async Task<List<StoryListModel>> MapAsync(List<Story> stories, string userId, ConnectedUsersModel connectedUsers)
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

        models.Sort((a, b) =>
        {
            int compare = (b.MaxDate ?? DateTime.MinValue).CompareTo(a.MaxDate ?? DateTime.MinValue);
            if (compare == 0)
                compare = (b.MinDate ?? DateTime.MinValue).CompareTo(a.MinDate ?? DateTime.MinValue);

            if (compare == 0)
                compare = a.Title.CompareTo(b.Title);

            return compare;
        });

        return models;
    }
}
