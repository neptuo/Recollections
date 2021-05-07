using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Neptuo;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Controllers
{
    [ApiController]
    [Route("api/profiles")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly ShareStatusService shareStatus;

        public ProfileController(UserManager<User> userManager, ShareStatusService shareStatus)
        {
            Ensure.NotNull(userManager, "userManager");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.userManager = userManager;
            this.shareStatus = shareStatus;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<ProfileModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetAsync(string id) => RunProfileAsync(id, Permission.Read, async (entity) =>
        {
            ProfileModel model = new ProfileModel();
            MapEntityToModel(entity, model);

            AuthorizedModel<ProfileModel> result = new AuthorizedModel<ProfileModel>(model);
            result.OwnerId = entity.Id;
            result.OwnerName = entity.UserName;
            result.UserPermission = entity.Id == HttpContext.User.FindUserId() ? Permission.Write : Permission.Read;

            return Ok(result);
        });

        private void MapEntityToModel(User entity, ProfileModel model)
        {
            model.RegistrationDate = entity.Created;
        }

        protected Task<IActionResult> RunProfileAsync(string profileId, Func<User, Task<IActionResult>> handler)
            => RunProfileAsync(profileId, null, handler);

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
                else if (sharePermission == Permission.Write)
                    return Unauthorized();
            }

            return await handler(entity);
        }
    }
}
