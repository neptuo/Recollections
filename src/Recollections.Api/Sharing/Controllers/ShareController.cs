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
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using System.Runtime.CompilerServices;

namespace Neptuo.Recollections.Sharing.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShareController : Entries.Controllers.ControllerBase
    {
        private readonly DataContext db;
        private readonly AccountsDataContext accountsDb;
        private readonly IUserNameProvider userNames;
        private readonly ShareCreator shareCreator;

        public ShareController(DataContext db, AccountsDataContext accountsDb, IUserNameProvider userNames, ShareStatusService shareStatus, ShareCreator shareCreator)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(accountsDb, "accountsDb");
            Ensure.NotNull(userNames, "userNames");
            this.db = db;
            this.accountsDb = accountsDb;
            this.userNames = userNames;
            this.shareCreator = shareCreator;
        }

        private async Task<IActionResult> GetItemsAsync(string ownerId, IQueryable<ShareBase> query, bool limitToPublicOnly = false)
        {
            List<string> otherUserIds = null;
            List<string> otherUserNames = null;
            if (limitToPublicOnly)
            {
                otherUserIds = new List<string>();
                otherUserNames = new List<string>();
            }
            else 
            {
                otherUserIds = await accountsDb.Connections
                    .Where(c => c.State == 2) // Active only
                    .Where(c => c.UserId == ownerId || c.OtherUserId == ownerId)
                    .Select(c => c.UserId == ownerId ? c.OtherUserId : c.UserId)
                    .ToListAsync();
                    
                otherUserNames = (await userNames.GetUserNamesAsync(otherUserIds)).ToList();
            }

            otherUserIds.Insert(0, ShareStatusService.PublicUserId);
            otherUserNames.Insert(0, ShareStatusService.PublicUserName);
            
            var savedItems = await query.ToListAsync();

            var result = new List<ShareModel>();
            for (int i = 0; i < otherUserIds.Count; i++)
            {
                var permission = (Permission?)savedItems.FirstOrDefault(s => s.UserId == otherUserIds[i])?.Permission;
                result.Add(new ShareModel(
                    otherUserNames[i],
                    permission
                ));
            }

            result.Sort((a, b) => 
            {
                if (a.UserName == ShareStatusService.PublicUserName)
                    return -1;
                else if (b.UserName == ShareStatusService.PublicUserName)
                    return 1;

                return a.UserName.CompareTo(b.UserName);
            });

            return Ok(result);
        }

        [HttpGet("entries/{entryId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetEntryAsync(string entryId) => RunEntryAsync(entryId, Permission.CoOwner, entry => GetItemsAsync(entry.UserId, db.EntryShares.Where(s => s.EntryId == entryId)));

        [HttpGet("stories/{storyId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetStoryAsync(string storyId) => RunStoryAsync(storyId, story => GetItemsAsync(story.UserId, db.StoryShares.Where(s => s.StoryId == storyId)));

        [HttpGet("beings/{beingId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetBeingAsync(string beingId) => RunBeingAsync(beingId, being => GetItemsAsync(being.UserId, db.BeingShares.Where(s => s.BeingId == beingId), HttpContext.User.FindUserId() == beingId));

        private async Task<IActionResult> ConvertResultAsync(Task<bool> result) => await result
            ? StatusCode(StatusCodes.Status200OK)
            : BadRequest();

        [HttpPut("entries/{entryId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> SaveEntryAsync(string entryId, List<ShareModel> models) => RunEntryAsync(entryId, Permission.CoOwner, entry => ConvertResultAsync(shareCreator.SaveEntryAsync(entry, models)));

        [HttpPut("stories/{storyId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> SaveStoryAsync(string storyId, List<ShareModel> models) => RunStoryAsync(storyId, story => ConvertResultAsync(shareCreator.SaveStoryAsync(story, models)));

        [HttpPut("beings/{beingId}/sharing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> SaveBeingAsync(string beingId, List<ShareModel> models) => RunBeingAsync(beingId, being => ConvertResultAsync(shareCreator.SaveBeingAsync(being, models)));
    }
}
