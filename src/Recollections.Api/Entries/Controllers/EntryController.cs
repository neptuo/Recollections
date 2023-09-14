using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    [ApiController]
    [Route("api/entries")]
    public class EntryController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ImageService imageService;
        private readonly ShareStatusService shareStatus;
        private readonly ShareDeleter shareDeleter;
        private readonly IUserNameProvider userNames;
        private readonly FreeLimitsChecker freeLimits;

        public EntryController(DataContext db, ImageService imageService, ShareStatusService shareStatus, ShareDeleter shareDeleter, IUserNameProvider userNames, FreeLimitsChecker freeLimits)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(imageService, "imageService");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(shareDeleter, "shareDeleter");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.db = db;
            this.imageService = imageService;
            this.shareStatus = shareStatus;
            this.shareDeleter = shareDeleter;
            this.userNames = userNames;
            this.freeLimits = freeLimits;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<EntryModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AuthorizedModel<EntryModel>>> Get([FromServices] ShareStatusService shareStatus, string id)
        {
            Ensure.NotNullOrEmpty(id, "id");

            Entry entity = await db.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            Permission? permission = await shareStatus.GetEntryPermissionAsync(entity, HttpContext.User.FindUserId());
            if (permission == null)
                return Unauthorized();

            EntryModel model = new EntryModel();
            MapEntityToModel(entity, model);

            var result = new AuthorizedModel<EntryModel>(model)
            {
                OwnerId = entity.UserId,
                OwnerName = await userNames.GetUserNameAsync(entity.UserId, HttpContext.User),
                UserPermission = permission.Value
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EntryModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (!await freeLimits.CanCreateEntryAsync(userId))
                return PremiumRequired();

            if (!await freeLimits.CanSetGpsAsync(userId, model.Locations.Count))
                return PremiumRequired();

            Entry entity = new Entry();
            MapModelToEntity(model, entity);
            entity.UserId = userId;
            entity.Created = DateTime.Now;

            await db.Entries.AddAsync(entity);
            await db.SaveChangesAsync();

            MapEntityToModel(entity, model);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, EntryModel model)
        {
            if (id != model.Id)
                return BadRequest();

            Entry entity = await db.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (!await shareStatus.IsEntrySharedAsCoOwnerAsync(id, userId))
                    return Unauthorized();
            }

            if (!await freeLimits.CanSetGpsAsync(userId, model.Locations.Count))
                return PremiumRequired();

            MapModelToEntity(model, entity);

            db.Entries.Update(entity);
            await db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entity = await db.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            if (!await shareStatus.IsEntrySharedAsCoOwnerAsync(id, userId))
                return Unauthorized();

            await imageService.DeleteAllAsync(entity);
            await shareDeleter.DeleteEntrySharesAsync(id);

            db.Entries.Remove(entity);
            await db.SaveChangesAsync();

            return Ok();
        }

        private void MapEntityToModel(Entry entity, EntryModel model)
        {
            model.Id = entity.Id;
            model.Title = entity.Title;
            model.When = entity.When;
            model.Text = entity.Text;
            model.Locations.AddRange(entity.Locations.OrderBy(l => l.Order).Select(l => new LocationModel()
            {
                Longitude = l.Longitude,
                Latitude = l.Latitude,
                Altitude = l.Altitude
            }));
        }

        private void MapModelToEntity(EntryModel model, Entry entity)
        {
            entity.Id = model.Id;
            entity.Title = model.Title;
            entity.When = model.When;
            entity.Text = model.Text;

            for (int i = 0; i < model.Locations.Count; i++)
            {
                if (i >= entity.Locations.Count)
                {
                    entity.Locations.Add(new OrderedLocation()
                    {
                        Order = i
                    });
                }

                OrderedLocation locationEntity = entity.Locations.First(l => l.Order == i);
                LocationModel location = model.Locations[i];

                locationEntity.Longitude = location.Longitude;
                locationEntity.Latitude = location.Latitude;
                locationEntity.Altitude = location.Altitude;
            }

            if (entity.Locations.Count > model.Locations.Count)
                entity.Locations.RemoveAt(entity.Locations.Count - 1);
        }
    }
}
