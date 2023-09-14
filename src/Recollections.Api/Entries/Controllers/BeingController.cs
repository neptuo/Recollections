using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/beings")]
    public class BeingController : ControllerBase
    {
        private readonly DataContext db;
        private readonly IUserNameProvider userNames;
        private readonly ShareStatusService shareStatus;
        private readonly ShareDeleter shareDeleter;
        private readonly FreeLimitsChecker freeLimits;

        public BeingController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus, ShareDeleter shareDeleter, FreeLimitsChecker freeLimits)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(shareDeleter, "shareDeleter");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.db = db;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.shareDeleter = shareDeleter;
            this.freeLimits = freeLimits;
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(List<BeingListModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<BeingListModel>>> GetList()
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            List<Being> entities = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Beings, userId)
                .OrderBy(b => b.Name)
                .ToListAsync();

            List<BeingListModel> models = new List<BeingListModel>();
            foreach (Being entity in entities)
            {
                var model = new BeingListModel();
                models.Add(model);

                MapEntityToModel(entity, model);

                int entries = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId)
                    .Where(e => e.Beings.Contains(entity))
                    .CountAsync();

                model.Entries = entries;
            }

            var userNames = await this.userNames.GetUserNamesAsync(models.Select(e => e.UserId).ToArray());
            for (int i = 0; i < models.Count; i++)
                models[i].UserName = userNames[i];

            return Ok(models);
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<BeingModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(string id) => RunBeingAsync(id, Permission.Read, async entity =>
        {
            Permission permission = Permission.CoOwner;
            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (!await shareStatus.IsBeingSharedForWriteAsync(id, userId))
                    permission = Permission.Read;
            }

            BeingModel model = new BeingModel();
            MapEntityToModel(entity, model);

            AuthorizedModel<BeingModel> result = new AuthorizedModel<BeingModel>(model);
            result.OwnerId = entity.UserId;
            result.OwnerName = await userNames.GetUserNameAsync(entity.UserId);
            result.UserPermission = permission;

            return Ok(result);
        });

        [HttpPost]
        public async Task<IActionResult> Create(BeingModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (!await freeLimits.CanCreateBeingAsync(userId))
                return PremiumRequired();

            Being entity = new Being();
            MapModelToEntity(model, entity);
            entity.UserId = userId;
            entity.Created = DateTime.Now;

            await db.Beings.AddAsync(entity);
            await db.SaveChangesAsync();

            MapEntityToModel(entity, model);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, model);
        }

        [HttpPut("{id}")]
        [ProducesDefaultResponseType(typeof(BeingModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Update(string id, BeingModel model) => RunBeingAsync(id, Permission.CoOwner, async entity =>
        {
            MapModelToEntity(model, entity);

            db.Beings.Update(entity);
            await db.SaveChangesAsync();

            return NoContent();
        });

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(string id) => RunBeingAsync(id, async entity =>
        {
            string userId = User.FindUserId();

            if (userId == entity.Id)
                return BadRequest();

            await shareDeleter.DeleteBeingSharesAsync(id);

            db.Beings.Remove(entity);
            await db.SaveChangesAsync();

            return Ok();
        });

        private void MapEntityToModel(Being entity, BeingListModel model)
        {
            model.Id = entity.Id;
            model.UserId = entity.UserId;
            model.Name = entity.Name;
            model.Icon = entity.Icon;
        }

        private void MapEntityToModel(Being entity, BeingModel model)
        {
            model.Id = entity.Id;
            model.UserId = entity.UserId;
            model.Name = entity.Name;
            model.Icon = entity.Icon;
            model.Text = entity.Text;
        }

        private void MapModelToEntity(BeingModel model, Being entity)
        {
            entity.Id = model.Id;
            entity.UserId = model.UserId;
            entity.Name = model.Name;
            entity.Icon = model.Icon;
            entity.Text = model.Text;
        }
    }
}
