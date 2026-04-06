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
        List<StoryListModel> models = new();
        foreach (var story in stories)
        {
            var model = new StoryListModel();
            models.Add(model);

            model.Id = story.Id;
            model.UserId = story.UserId;
            model.Title = story.Title;

            int chapters = await dataContext.Stories
                .Where(s => s.Id == story.Id)
                .SelectMany(s => s.Chapters)
                .CountAsync();

            var entries = await shareStatus.OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, [userId, ShareStatusService.PublicUserId], connectedUsers)
                .Where(e => e.Story.Id == story.Id || e.Chapter.Story.Id == story.Id)
                .Select(e => e.When)
                .ToListAsync();

            model.Chapters = chapters;
            model.Entries = entries.Count;

            if (entries.Count > 0)
            {
                model.MinDate = entries.Min();
                model.MaxDate = entries.Max();
            }
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
