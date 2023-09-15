using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Neptuo;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Accounts.Controllers
{
    [ApiController]
    [Route("api/profiles")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly ShareStatusService shareStatus;
        private readonly TimelineService timeline;

        public ProfileController(UserManager<User> userManager, ShareStatusService shareStatus, TimelineService timeline)
        {
            Ensure.NotNull(userManager, "userManager");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(timeline, "timeline");
            this.userManager = userManager;
            this.shareStatus = shareStatus;
            this.timeline = timeline;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<ProfileModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetAsync(string id) => RunProfileAsync(id, Permission.Read, (entity) =>
        {
            ProfileModel model = new ProfileModel();
            MapEntityToModel(entity, model);

            AuthorizedModel<ProfileModel> result = new AuthorizedModel<ProfileModel>(model);
            result.OwnerId = entity.Id;
            result.OwnerName = entity.UserName;
            result.UserPermission = entity.Id == HttpContext.User.FindUserId() ? Permission.CoOwner : Permission.Read;

            return Task.FromResult<IActionResult>(Ok(result));
        });

        [HttpGet("{id}/timeline/list")]
        public Task<IActionResult> GetEntriesAsync([FromServices] EntriesDataContext dataContext, string id, int offset) => RunProfileAsync(id, Permission.Read, async entity => 
        {
            var query = dataContext.Entries.Where(e => e.UserId == id);

            var (models, hasMore) = await timeline.GetAsync(query, id, Enumerable.Empty<string>(), offset);
            return Ok(new TimelineListResponse(models, hasMore));
        });

        private void MapEntityToModel(User entity, ProfileModel model)
        {
            model.RegistrationDate = entity.Created;
        }

        protected async Task<IActionResult> RunProfileAsync(string profileId, Permission? sharePermission, Func<User, Task<IActionResult>> handler)
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
                else if (sharePermission == Permission.CoOwner)
                    return Unauthorized();
            }
            
            return await handler(entity);
        }
    }
}
