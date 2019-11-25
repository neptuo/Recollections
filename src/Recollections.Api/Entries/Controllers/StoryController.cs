using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Entries.Stories;
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
    public class StoryController : Controller
    {
        private readonly DataContext dataContext;

        public StoryController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<StoryListModel>>> GetList()
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            List<Story> entities = await dataContext.Stories
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Order)
                .ToListAsync();

            List<StoryListModel> models = new List<StoryListModel>();
            foreach (Story entity in entities)
            {
                var model = new StoryListModel();
                models.Add(model);

                MapEntityToModel(entity, model);

                int chapters = await dataContext.Stories
                    .Where(s => s.Id == entity.Id)
                    .SelectMany(s => s.Chapters)
                    .CountAsync();

                int entries = await dataContext.Entries
                    .Where(e => e.Story.Id == entity.Id)
                    .CountAsync();

                entries += await dataContext.Entries
                    .Where(e => e.Chapter.Story.Id == entity.Id)
                    .CountAsync();

                model.Chapters = chapters;
                model.Entries = entries;

                if (entries > 0)
                {
                    DateTime minDate = await dataContext.Entries
                        .Where(e => e.Story.Id == entity.Id || e.Chapter.Story.Id == entity.Id)
                        .MinAsync(e => e.When);

                    DateTime maxDate = await dataContext.Entries
                        .Where(e => e.Story.Id == entity.Id || e.Chapter.Story.Id == entity.Id)
                        .MaxAsync(e => e.When);

                    model.MinDate = minDate;
                    model.MaxDate = maxDate;
                }
            }

            return Ok(models);
        }

        [HttpGet("{storyId}/chapters")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<StoryChapterListModel>>> GetChapterList(string storyId)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            List<StoryChapter> entities = await dataContext.Stories
                .Where(s => s.UserId == userId)
                .Where(s => s.Id == storyId)
                .SelectMany(s => s.Chapters)
                .OrderBy(c => c.Order)
                .ToListAsync();

            List<StoryChapterListModel> models = new List<StoryChapterListModel>();
            foreach (StoryChapter entity in entities)
            {
                models.Add(new StoryChapterListModel()
                {
                    Id = entity.Id,
                    Title = entity.Title
                });
            }

            return Ok(models);
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StoryModel>> Get(string id)
        {
            Ensure.NotNullOrEmpty(id, "id");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Story entity = await dataContext.Stories.Include(s => s.Chapters).FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            StoryModel model = new StoryModel();
            MapEntityToModel(entity, model);

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(StoryModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Story entity = new Story();
            MapModelToEntity(model, entity);
            entity.UserId = userId;
            entity.Order = await dataContext.Stories.CountAsync(s => s.UserId == userId) + 1;
            entity.Created = DateTime.Now;

            await dataContext.Stories.AddAsync(entity);
            await dataContext.SaveChangesAsync();

            MapEntityToModel(entity, model);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StoryModel>> Update(string id, StoryModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (id != model.Id)
                return BadRequest();

            Story entity = await dataContext.Stories.Include(s => s.Chapters).FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            var removedChapters = MapModelToEntity(model, entity);
            foreach (var chapter in removedChapters)
            {
                foreach (var entry in await dataContext.Entries.Where(e => e.Chapter.Id == chapter.Id).ToListAsync())
                {
                    entry.Chapter = null;
                    dataContext.Entries.Update(entry);
                }
            }

            dataContext.Stories.Update(entity);
            await dataContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Story entity = await dataContext.Stories
                .Include(s => s.Chapters)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            foreach (var entry in await dataContext.Entries.Where(e => e.Story.Id == id).ToListAsync())
            {
                entry.Story = null;
                dataContext.Entries.Update(entry);
            }

            foreach (var entry in await dataContext.Entries.Where(e => e.Chapter.Story.Id == id).ToListAsync())
            {
                entry.Chapter = null;
                dataContext.Entries.Update(entry);
            }

            foreach (var story in await dataContext.Stories.Where(e => e.UserId == userId && e.Order > entity.Order).ToListAsync())
            {
                story.Order--;
                dataContext.Stories.Update(story);
            }

            foreach (var chapter in entity.Chapters)
                dataContext.Remove(chapter);

            dataContext.Stories.Remove(entity);
            await dataContext.SaveChangesAsync();

            return Ok();
        }

        private void MapEntityToModel(Story entity, StoryListModel model)
        {
            model.Id = entity.Id;
            model.Title = entity.Title;
        }

        private void MapEntityToModel(Story entity, StoryModel model)
        {
            model.Id = entity.Id;
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
