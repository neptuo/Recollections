using Microsoft.AspNetCore.Http;
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

        public StoryController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus, ShareDeleter shareDeleter, FreeLimitsChecker freeLimits)
            : base(db, shareStatus, runStoryObserver: RunStoryModifier)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(shareDeleter, "shareDeleter");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.db = db;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.shareDeleter = shareDeleter;
            this.freeLimits = freeLimits;
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

            List<Story> entities = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Stories, userId)
                .OrderByDescending(s => s.Order)
                .ToListAsync();

            List<StoryListModel> models = new List<StoryListModel>();
            foreach (Story entity in entities)
            {
                var model = new StoryListModel();
                models.Add(model);

                MapEntityToModel(entity, model);

                int chapters = await db.Stories
                    .Where(s => s.Id == entity.Id)
                    .SelectMany(s => s.Chapters)
                    .CountAsync();

                int entries = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId)
                    .Where(e => e.Story.Id == entity.Id || e.Chapter.Story.Id == entity.Id)
                    .CountAsync();

                model.Chapters = chapters;
                model.Entries = entries;

                if (entries > 0)
                {
                    DateTime minDate = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId)
                        .Where(e => e.Story.Id == entity.Id || e.Chapter.Story.Id == entity.Id)
                        .MinAsync(e => e.When);

                    DateTime maxDate = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId)
                        .Where(e => e.Story.Id == entity.Id || e.Chapter.Story.Id == entity.Id)
                        .MaxAsync(e => e.When);

                    model.MinDate = minDate;
                    model.MaxDate = maxDate;
                }
            }

            var userNames = await this.userNames.GetUserNamesAsync(models.Select(e => e.UserId).ToArray());
            for (int i = 0; i < models.Count; i++)
                models[i].UserName = userNames[i];

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
        public Task<IActionResult> Get(string id) => RunStoryAsync(id, Permission.Read, async entity =>
        {
            Permission permission = Permission.CoOwner;
            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (!await shareStatus.IsStorySharedForWriteAsync(id, userId))
                    permission = Permission.Read;
            }

            StoryModel model = new StoryModel();
            MapEntityToModel(entity, model);

            AuthorizedModel<StoryModel> result = new AuthorizedModel<StoryModel>(model);
            result.OwnerId = entity.UserId;
            result.OwnerName = await userNames.GetUserNameAsync(entity.UserId);
            result.UserPermission = permission;

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

            Story entity = new Story();
            MapModelToEntity(model, entity);
            entity.UserId = userId;
            entity.Order = await db.Stories.CountAsync(s => s.UserId == userId) + 1;
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
        public Task<IActionResult> Delete(string id) => RunStoryAsync(id, async entity =>
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

            foreach (var story in await db.Stories.Where(e => e.UserId == userId && e.Order > entity.Order).ToListAsync())
            {
                story.Order--;
                db.Stories.Update(story);
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
