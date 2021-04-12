using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Sharing.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShareController : Entries.Controllers.ControllerBase
    {
        private readonly DataContext db;
        private readonly IUserNameProvider userNames;

        public ShareController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            this.db = db;
            this.userNames = userNames;
        }

        private async Task<IActionResult> GetItemsAsync(IQueryable<ShareBase> query)
        {
            var items = await query
                .Select(s => new ShareModel()
                {
                    UserName = s.UserId,
                    Permission = (Permission)s.Permission
                })
                .ToListAsync();

            if (items.Count > 0)
            {
                var p = items.FirstOrDefault(s => s.UserName == ShareStatusService.PublicUserName);
                if (p != null)
                    items.Remove(p);

                var names = await userNames.GetUserNamesAsync(items.Select(s => s.UserName).ToArray());
                for (int i = 0; i < items.Count; i++)
                    items[i].UserName = names[i];

                items.Sort((a, b) => a.UserName.CompareTo(b.UserName));

                if (p != null)
                    items.Insert(0, p);
            }

            return Ok(items);
        }

        [HttpGet("entries/{entryId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetEntryAsync(string entryId) => RunEntryAsync(entryId, entry =>
        {
            return GetItemsAsync(db.EntryShares.Where(s => s.EntryId == entryId));
        });

        [HttpGet("stories/{storyId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetStoryAsync(string storyId) => RunStoryAsync(storyId, story =>
        {
            return GetItemsAsync(db.StoryShares.Where(s => s.StoryId == storyId));
        });

        [HttpGet("beings/{beingId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetBeingAsync(string beingId) => RunBeingAsync(beingId, being =>
        {
            return GetItemsAsync(db.BeingShares.Where(s => s.BeingId == beingId));
        });

        private async Task<IActionResult> CreateAsync<T>(ShareModel model, Func<string, IQueryable<T>> findQuery, Func<T> entityFactory)
            where T : ShareBase
        {
            string userId;
            if (model.UserName != null && model.UserName != ShareStatusService.PublicUserName)
                userId = (await userNames.GetUserIdsAsync(new[] { model.UserName })).First();
            else
                userId = ShareStatusService.PublicUserId;

            if (userId == ShareStatusService.PublicUserId && model.Permission != Permission.Read)
                return BadRequest();

            T entity = await findQuery(userId).FirstOrDefaultAsync();
            if (entity == null)
            {
                entity = entityFactory();
                entity.UserId = userId;
                entity.Permission = (int)model.Permission;

                await db.Set<T>().AddAsync(entity);
            }
            else
            {
                entity.Permission = (int)model.Permission;
            }

            await db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost("entries/{entryId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateEntryAsync(string entryId, ShareModel model) => RunEntryAsync(entryId, entry =>
        {
            return CreateAsync(
                model, 
                userId => db.EntryShares.Where(s => s.EntryId == entryId && s.UserId == userId), 
                () => new EntryShare(entryId)
            );
        });

        [HttpPost("stories/{storyId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateStoryAsync(string storyId, ShareModel model) => RunStoryAsync(storyId, story =>
        {
            return CreateAsync(
                model,
                userId => db.StoryShares.Where(s => s.StoryId == storyId && s.UserId == userId),
                () => new StoryShare(storyId)
            );
        });

        [HttpPost("beings/{beingId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateBeingAsync(string beingId, ShareModel model) => RunBeingAsync(beingId, being =>
        {
            return CreateAsync(
                model,
                userId => db.BeingShares.Where(s => s.BeingId == beingId && s.UserId == userId),
                () => new BeingShare(beingId)
            );
        });

        private async Task<IActionResult> DeleteAsync<T>(string userName, Func<string, IQueryable<T>> findQuery)
            where T : ShareBase
        {
            string userId;
            if (userName != null && userName != ShareStatusService.PublicUserName)
                userId = (await userNames.GetUserIdsAsync(new[] { userName })).First();
            else
                userId = ShareStatusService.PublicUserId;

            T entity = await findQuery(userId).FirstOrDefaultAsync();
            if (entity == null)
                return NotFound();

            db.Set<T>().Remove(entity);
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("entries/{entryId}/sharing/{userName}")]
        public Task<IActionResult> DeleteEntryAsync(string entryId, string userName) => RunEntryAsync(entryId, async entry =>
        {
            return await DeleteAsync(
                userName, 
                userId => db.EntryShares.Where(s => s.EntryId == entryId && s.UserId == userId)
            );
        });

        [HttpDelete("stories/{storyId}/sharing/{userName}")]
        public Task<IActionResult> DeleteStoryAsync(string storyId, string userName) => RunStoryAsync(storyId, async story =>
        {
            return await DeleteAsync(
                userName,
                userId => db.StoryShares.Where(s => s.StoryId == storyId && s.UserId == userId)
            );
        });

        [HttpDelete("beings/{beingId}/sharing/{userName}")]
        public Task<IActionResult> DeleteBeingAsync(string beingId, string userName) => RunBeingAsync(beingId, async being =>
        {
            return await DeleteAsync(
                userName,
                userId => db.BeingShares.Where(s => s.BeingId == beingId && s.UserId == userId)
            );
        });
    }
}
