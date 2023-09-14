﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
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
    [Route("api/stories/{storyId}")]
    public class StoryEntriesController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;

        public StoryEntriesController(DataContext db, ShareStatusService shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.db = db;
            this.shareStatus = shareStatus;
        }

        [HttpGet("entries")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public async Task<ActionResult<List<StoryEntryModel>>> GetStoryEntryList(string storyId)
        {
            string userId = User.FindUserId();
            var models = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId)
                .Where(e => e.Story.Id == storyId)
                .OrderBy(e => e.When)
                .Select(e => new StoryEntryModel()
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToListAsync();

            return Ok(models);
        }

        [HttpGet("chapters/{chapterId}/entries")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public async Task<ActionResult<List<StoryEntryModel>>> GetChapterEntryList(string storyId, string chapterId)
        {
            string userId = User.FindUserId();
            var models = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId)
                .Where(e => e.Chapter.Story.Id == storyId && e.Chapter.Id == chapterId)
                .OrderBy(e => e.When)
                .Select(e => new StoryEntryModel()
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToListAsync();

            return Ok(models);
        }
    }
}
