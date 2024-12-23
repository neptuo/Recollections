﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/stories")]
    public class StoryController : ControllerBase
    {
        private readonly DataContext db;
        private readonly IUserNameProvider userNames;
        private readonly ShareStatusService shareStatus;
        private readonly ShareDeleter shareDeleter;
        private readonly FreeLimitsChecker freeLimits;
        private readonly IConnectionProvider connections;

        public StoryController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus, ShareDeleter shareDeleter, FreeLimitsChecker freeLimits, IConnectionProvider connections)
            : base(db, shareStatus, runStoryObserver: RunStoryModifier)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(shareDeleter, "shareDeleter");
            Ensure.NotNull(freeLimits, "freeLimits");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.shareDeleter = shareDeleter;
            this.freeLimits = freeLimits;
            this.connections = connections;
        }

        private static IQueryable<Story> RunStoryModifier(IQueryable<Story> query)
            => query.Include(s => s.Chapters);

        [HttpGet]
        [ProducesDefaultResponseType(typeof(List<StoryListModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<StoryListModel>>> GetList()
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            List<Story> entities = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Stories, userId, connectedUsers)
                .ToListAsync();

            List<StoryListModel> models = new();
            foreach (Story entity in entities)
            {
                var model = new StoryListModel();
                models.Add(model);

                MapEntityToModel(entity, model);

                int chapters = await db.Stories
                    .Where(s => s.Id == entity.Id)
                    .SelectMany(s => s.Chapters)
                    .CountAsync();

                var entries = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, [userId, ShareStatusService.PublicUserId], connectedUsers)
                    .Where(e => e.Story.Id == entity.Id || e.Chapter.Story.Id == entity.Id)
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

            var userNames = await this.userNames.GetUserNamesAsync(models.Select(e => e.UserId).ToArray());
            for (int i = 0; i < models.Count; i++)
                models[i].UserName = userNames[i];

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
        }

        [HttpGet("{storyId}/chapters")]
        [ProducesDefaultResponseType(typeof(List<StoryChapterListModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetChapterList(string storyId) => RunStoryAsync(storyId, Permission.Read, story =>
        {
            List<StoryChapterListModel> models = new List<StoryChapterListModel>();
            foreach (StoryChapter entity in story.Chapters.OrderBy(c => c.Order))
            {
                models.Add(new StoryChapterListModel()
                {
                    Id = entity.Id,
                    Title = entity.Title
                });
            }

            return Task.FromResult<IActionResult>(Ok(models));
        });

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<StoryModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(string id) => RunStoryAsync(id, Permission.Read, async (entity, permission) =>
        {
            StoryModel model = new StoryModel();
            MapEntityToModel(entity, model);

            AuthorizedModel<StoryModel> result = new AuthorizedModel<StoryModel>(model)
            {
                OwnerId = entity.UserId,
                OwnerName = await userNames.GetUserNameAsync(entity.UserId),
                UserPermission = permission
            };

            return Ok(result);
        });

        [HttpPost]
        public async Task<IActionResult> Create(StoryModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (!await freeLimits.CanCreateStoryAsync(userId))
                return PremiumRequired();

            Story entity = new Story()
            {
                IsSharingInherited = true
            };
            MapModelToEntity(model, entity);
            entity.UserId = userId;
            entity.Created = DateTime.Now;

            await db.Stories.AddAsync(entity);
            await db.SaveChangesAsync();

            MapEntityToModel(entity, model);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, model);
        }

        [HttpPut("{id}")]
        [ProducesDefaultResponseType(typeof(StoryModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Update(string id, StoryModel model) => RunStoryAsync(id, Permission.CoOwner, async entity =>
        {
            var removedChapters = MapModelToEntity(model, entity);
            foreach (var chapter in removedChapters)
            {
                foreach (var entry in await db.Entries.Where(e => e.Chapter.Id == chapter.Id).ToListAsync())
                {
                    entry.Chapter = null;
                    db.Entries.Update(entry);
                }
            }

            db.Stories.Update(entity);
            await db.SaveChangesAsync();

            return NoContent();
        });

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(string id) => RunStoryAsync(id, Permission.CoOwner, async entity =>
        {
            string userId = User.FindUserId();

            foreach (var entry in await db.Entries.Where(e => e.Story.Id == id).ToListAsync())
            {
                entry.Story = null;
                db.Entries.Update(entry);
            }

            foreach (var entry in await db.Entries.Where(e => e.Chapter.Story.Id == id).ToListAsync())
            {
                entry.Chapter = null;
                db.Entries.Update(entry);
            }

            foreach (var chapter in entity.Chapters)
                db.Remove(chapter);

            await shareDeleter.DeleteStorySharesAsync(id);

            db.Stories.Remove(entity);
            await db.SaveChangesAsync();

            return Ok();
        });

        private void MapEntityToModel(Story entity, StoryListModel model)
        {
            model.Id = entity.Id;
            model.UserId = entity.UserId;
            model.Title = entity.Title;
        }

        private void MapEntityToModel(Story entity, StoryModel model)
        {
            model.Id = entity.Id;
            model.UserId = entity.UserId;
            model.Title = entity.Title;
            model.Text = entity.Text;

            foreach (var chapterEntity in entity.Chapters.OrderBy(c => c.Order))
            {
                model.Chapters.Add(new ChapterModel()
                {
                    Id = chapterEntity.Id,
                    Title = chapterEntity.Title,
                    Text = chapterEntity.Text
                });
            }
        }

        private IReadOnlyCollection<StoryChapter> MapModelToEntity(StoryModel model, Story entity)
        {
            entity.Id = model.Id;
            entity.Title = model.Title;
            entity.Text = model.Text;

            for (int i = 0; i < model.Chapters.Count; i++)
            {
                var chapterModel = model.Chapters[i];

                var chapterEntity = entity.Chapters.FirstOrDefault(c => c.Id == chapterModel.Id);
                if (chapterEntity == null)
                {
                    entity.Chapters.Add(chapterEntity = new StoryChapter()
                    {
                        Created = DateTime.Now
                    });
                }

                chapterEntity.Id = chapterModel.Id;
                chapterEntity.Title = chapterModel.Title;
                chapterEntity.Text = chapterModel.Text;
                chapterEntity.Order = i + 1;
            }

            List<StoryChapter> toRemove = new List<StoryChapter>();
            foreach (var chapterEntity in entity.Chapters)
            {
                if (!model.Chapters.Any(c => c.Id == chapterEntity.Id))
                    toRemove.Add(chapterEntity);
            }

            foreach (var chapterEntity in toRemove)
                entity.Chapters.Remove(chapterEntity);

            return toRemove;
        }
    }
}
