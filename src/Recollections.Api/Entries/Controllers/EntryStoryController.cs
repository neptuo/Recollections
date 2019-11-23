﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
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
    [Route("api/entries/{entryId}/story")]
    public class EntryStoryController : ControllerBase
    {
        private readonly DataContext dataContext;

        public EntryStoryController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }

        private async Task<IActionResult> RunEntryAsync(string entryId, Func<Entry, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(entryId, "entryId");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entity = await dataContext.Entries
                .Include(e => e.Chapter)
                .Include(e => e.Chapter.Story)
                .Include(e => e.Story)
                .FirstOrDefaultAsync(e => e.Id == entryId);

            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            return await handler(entity);
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> Get(string entryId) => RunEntryAsync(entryId, entry =>
        {
            StoryChapter chapter = null;
            Story story = null;
            if (entry.Chapter != null)
            {
                chapter = entry.Chapter;
                story = entry.Chapter.Story;
            }
            else if (entry.Story != null)
            {
                story = entry.Story;
            }

            var model = new EntryStoryModel()
            {
                StoryId = story?.Id,
                StoryTitle = story?.Title,
                ChapterId = chapter?.Id,
                ChapterTitle = chapter?.Title
            };

            return Task.FromResult<IActionResult>(Ok(model));
        });

        [HttpPut]
        public Task<IActionResult> Update(string entryId, EntryStoryUpdateModel model) => RunEntryAsync(entryId, async entry =>
        {
            string userId = HttpContext.User.FindUserId();
            Story story = null;
            StoryChapter chapter = null;

            if (model.StoryId != null)
            {
                story = await dataContext.Stories
                    .Where(s => s.UserId == userId && s.Id == model.StoryId)
                    .Include(s => s.Chapters)
                    .FirstOrDefaultAsync();

                if (story == null)
                {
                    model.StoryId = null;
                    model.ChapterId = null;
                }
            }

            if (model.ChapterId != null)
            {
                chapter = story.Chapters.FirstOrDefault(c => c.Id == model.ChapterId);
                if (chapter == null)
                    story = null;
            }

            if (story == null)
            {
                entry.Story = null;
                entry.Chapter = null;
            }
            else if (chapter == null)
            {
                entry.Story = story;
                entry.Chapter = null;
            }
            else
            {
                entry.Story = null;
                entry.Chapter = chapter;
            }

            dataContext.Entries.Update(entry);
            await dataContext.SaveChangesAsync();

            return NoContent();
        });
    }
}
