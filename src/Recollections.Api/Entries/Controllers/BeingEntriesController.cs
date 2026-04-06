using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/beings/{beingId}")]
    public class BeingEntriesController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly EntryListMapper entryMapper;
        private readonly IUserNameProvider userNames;
        private readonly IConnectionProvider connections;

        public BeingEntriesController(DataContext db, ShareStatusService shareStatus, EntryListMapper entryMapper, IUserNameProvider userNames, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.shareStatus = shareStatus;
            this.entryMapper = entryMapper;
            this.userNames = userNames;
            this.connections = connections;
        }

        [HttpGet("timeline")]
        [ProducesDefaultResponseType(typeof(PageableList<EntryListModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> List(string beingId, int offset) => RunBeingAsync(beingId, Permission.Read, async being =>
        {
            var userId = User.FindUserId();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
                db, 
                db.Entries
                    .Where(e => e.Beings.Any(b => b.Id == beingId))
                    .OrderByDescending(e => e.When), 
                [userId, ShareStatusService.PublicUserId], 
                connectedUsers
            );

            var (models, hasMore) = await entryMapper.MapAsync(query, userId, connectedUsers, offset);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        });

        [HttpGet("stories")]
        [ProducesDefaultResponseType(typeof(List<StoryListModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetStories(string beingId) => RunBeingAsync(beingId, Permission.Read, async being =>
        {
            var userId = User.FindUserId();
            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            var storyIds = await shareStatus.OwnedByOrExplicitlySharedWithUser(
                    db,
                    db.Entries
                        .Where(e => e.Beings.Any(b => b.Id == beingId))
                        .Where(e => e.Story != null || e.Chapter != null),
                    [userId, ShareStatusService.PublicUserId],
                    connectedUsers
                )
                .Select(e => e.Story != null ? e.Story.Id : e.Chapter.Story.Id)
                .Distinct()
                .ToListAsync();

            var stories = await db.Stories
                .Where(s => storyIds.Contains(s.Id))
                .ToListAsync();

            List<StoryListModel> models = new();
            foreach (var story in stories)
            {
                var model = new StoryListModel();
                models.Add(model);

                model.Id = story.Id;
                model.UserId = story.UserId;
                model.Title = story.Title;

                int chapters = await db.Stories
                    .Where(s => s.Id == story.Id)
                    .SelectMany(s => s.Chapters)
                    .CountAsync();

                var entries = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, [userId, ShareStatusService.PublicUserId], connectedUsers)
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

            return Ok(models);
        });
    }
}
