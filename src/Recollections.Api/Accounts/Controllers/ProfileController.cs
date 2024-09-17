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
    public class ProfileController : Neptuo.Recollections.Entries.Controllers.ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly ShareStatusService shareStatus;
        private readonly TimelineService timeline;

        public ProfileController(UserManager<User> userManager, EntriesDataContext entriesDb, ShareStatusService shareStatus, TimelineService timeline)
            : base(entriesDb, shareStatus)
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
        public Task<IActionResult> GetAsync(string id) => RunBeingAsync(id, Permission.Read, async _ =>
        {
            ProfileModel model = new ProfileModel();
            User entity = await userManager.FindByIdAsync(id);

            MapEntityToModel(entity, model);

            AuthorizedModel<ProfileModel> result = new AuthorizedModel<ProfileModel>(model);
            result.OwnerId = entity.Id;
            result.OwnerName = entity.UserName;
            result.UserPermission = entity.Id == HttpContext.User.FindUserId() ? Permission.CoOwner : Permission.Read;

            return Ok(result);
        });

        [HttpGet("{id}/timeline/list")]
        public Task<IActionResult> GetEntriesAsync([FromServices] EntriesDataContext dataContext, [FromServices] IConnectionProvider connections, string id, int offset) => RunBeingAsync(id, Permission.Read, async _ =>
        {
            var userId = User.FindUserId();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries.Where(e => e.UserId == id).OrderByDescending(e => e.When), userId, connectedUsers);

            var (models, hasMore) = await timeline.GetAsync(query, id, connectedUsers, offset);
            return Ok(new TimelineListResponse(models, hasMore));
        });

        private void MapEntityToModel(User entity, ProfileModel model)
        {
            model.RegistrationDate = entity.Created;
        }
    }
}
