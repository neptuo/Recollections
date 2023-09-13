using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        private readonly ShareCreator shareCreator;

        public ShareController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus, ShareCreator shareCreator)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            this.db = db;
            this.userNames = userNames;
            this.shareCreator = shareCreator;
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

        [HttpGet("profiles/{profileId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetProfileAsync([FromServices] ShareStatusService shareStatus, [FromServices] UserManager<User> userManager, string profileId) => RunProfileAsync(shareStatus, userManager, profileId, profile =>
        {
            return GetItemsAsync(db.ProfileShares.Where(s => s.ProfileId == profileId));
        });

        // DUPLICATED CODE FROM ProfileController.cs

        protected Task<IActionResult> RunProfileAsync(ShareStatusService shareStatus, UserManager<User> userManager, string profileId, Func<User, Task<IActionResult>> handler)
            => RunProfileAsync(shareStatus, userManager, profileId, null, handler);

        protected async Task<IActionResult> RunProfileAsync(ShareStatusService shareStatus, UserManager<User> userManager, string profileId, Permission? sharePermission, Func<User, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(profileId, "profileId");

            User entity = await userManager.FindByIdAsync(profileId);
            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (entity.Id != userId)
            {
                if (sharePermission == null)
                    return Unauthorized();
                else if (sharePermission == Permission.Read && !await shareStatus.IsProfileSharedForReadAsync(profileId, userId))
                    return Unauthorized();
                else if (sharePermission == Permission.Write)
                    return Unauthorized();
            }

            return await handler(entity);
        }

        // /DUPLICATED CODE FROM ProfileController.cs


        private async Task<IActionResult> ConvertResultAsync(Task<bool> result) => await result
            ? StatusCode(StatusCodes.Status201Created)
            : BadRequest();

        [HttpPost("entries/{entryId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateEntryAsync(string entryId, ShareModel model) => RunEntryAsync(entryId, entry => ConvertResultAsync(shareCreator.CreateEntryAsync(entry, model)));

        [HttpPost("stories/{storyId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateStoryAsync(string storyId, ShareModel model) => RunStoryAsync(storyId, story => ConvertResultAsync(shareCreator.CreateStoryAsync(story, model)));

        [HttpPost("beings/{beingId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateBeingAsync(string beingId, ShareModel model) => RunBeingAsync(beingId, async being =>
        {
            if (being.Id == being.UserId && model.UserName != null)
                return BadRequest();

            return await ConvertResultAsync(shareCreator.CreateBeingAsync(being, model));
        });

        [HttpPost("profiles/{profileId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateProfileAsync([FromServices] ShareStatusService shareStatus, [FromServices] UserManager<User> userManager, string profileId, ShareModel model) => RunProfileAsync(shareStatus, userManager, profileId, profile =>
        {
            // return shareCreator.CreateAsync(
            //     model,
            //     userId => db.ProfileShares.Where(s => s.ProfileId == profileId && s.UserId == userId),
            //     () => new ProfileShare(profileId)
            // );
            throw new NotSupportedException("Profile sharing is discontinued");
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
            if (being.Id == being.UserId && userName != ShareStatusService.PublicUserName)
                return BadRequest();

            return await DeleteAsync(
                userName,
                userId => db.BeingShares.Where(s => s.BeingId == beingId && s.UserId == userId)
            );
        });

        [HttpDelete("profiles/{profileId}/sharing/{userName}")]
        public Task<IActionResult> DeleteProfileAsync([FromServices] ShareStatusService shareStatus, [FromServices] UserManager<User> userManager, string profileId, string userName) => RunProfileAsync(shareStatus, userManager, profileId, async profile =>
        {
            return await DeleteAsync(
                userName,
                userId => db.ProfileShares.Where(s => s.ProfileId == profileId && s.UserId == userId)
            );
        });
    }
}
