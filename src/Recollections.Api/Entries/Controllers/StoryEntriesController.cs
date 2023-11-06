﻿using Microsoft.AspNetCore.Mvc;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/stories/{storyId}")]
    public class StoryEntriesController : ControllerBase
    {
        private readonly DataContext db;
        private readonly TimelineService timeline;

        public StoryEntriesController(DataContext db, ShareStatusService shareStatus, TimelineService timeline)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(timeline, "timeline");
            this.db = db;
            this.timeline = timeline;
        }

        [HttpGet("timeline")]
        [ProducesDefaultResponseType(typeof(TimelineListResponse))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetStoryTimeline(string storyId) => RunStoryAsync(storyId, Permission.Read, async story =>
        {
            var userId = User.FindUserId();
            
            var query = db.Entries.Where(e => e.Story.Id == storyId).OrderBy(e => e.When);

            var (models, hasMore) = await timeline.GetAsync(query, userId, Enumerable.Empty<string>(), null);
            return Ok(new TimelineListResponse(models, hasMore));
        });

        [HttpGet("chapters/{chapterId}/timeline")]
        [ProducesDefaultResponseType(typeof(TimelineListResponse))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetChapterTimeline(string storyId, string chapterId) => RunStoryAsync(storyId, Permission.Read, async story =>
        {
            var userId = User.FindUserId();
            
            var query = db.Entries.Where(e => e.Chapter.Id == chapterId).OrderBy(e => e.When);

            var (models, hasMore) = await timeline.GetAsync(query, userId, Enumerable.Empty<string>(), null);
            return Ok(new TimelineListResponse(models, hasMore));
        });
    }
}
