using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [Authorize]
    [Route("api/timeline/[action]")]
    public class TimelineController : ControllerBase
    {
        private const int PageSize = 10;

        private readonly DataContext dataContext;
        private readonly IUserNameProvider userNames;
        private readonly ShareStatusService shareStatus;

        public TimelineController(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.dataContext = dataContext;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
        }

        [HttpGet]
        public async Task<IActionResult> List(int offset)
        {
            Ensure.PositiveOrZero(offset, "offset");

            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId)
                .OrderByDescending(e => e.When)
                .Skip(offset)
                .Take(PageSize)
                .Select(e => new
                {
                    Model = new TimelineEntryModel()
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Title = e.Title,
                        When = e.When,
                        StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                        ChapterTitle = e.Chapter.Title,
                        GpsCount = e.Locations.Count,
                        ImageCount = dataContext.Images.Count(i => i.Entry.Id == e.Id),
                        //Beings = e.Beings
                        //    .Select(b => new TimelineEntryBeingModel()
                        //    {
                        //        Name = b.Name,
                        //        Icon = b.Icon
                        //    }).ToList()
                    },
                    BeingCount = e.Beings.Count(),
                    Text = e.Text
                })
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            foreach (var entry in result)
            {
                if (entry.BeingCount > 0) 
                {
                    entry.Model.Beings = await shareStatus
                        .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId)
                        .Where(e => e.Id == entry.Model.Id)
                        .SelectMany(e => e.Beings)
                        .OrderBy(b => b.Name)
                        .Select(b => new TimelineEntryBeingModel()
                        {
                            Name = b.Name,
                            Icon = b.Icon
                        })
                        .ToListAsync();
                }

                if (!String.IsNullOrEmpty(entry.Text))
                    entry.Model.TextWordCount = entry.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            }

            var userNames = await this.userNames.GetUserNamesAsync(result.Select(e => e.Model.UserId).ToArray());
            for (int i = 0; i < result.Count; i++)
                result[i].Model.UserName = userNames[i];

            return Ok(new TimelineListResponse(result.Select(e => e.Model).ToList(), result.Count == PageSize));
        }
    }
}
