using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
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

        public EntryController(DataContext db, ImageService imageService, ShareStatusService shareStatus, ShareDeleter shareDeleter)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(imageService, "imageService");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(shareDeleter, "shareDeleter");
            this.db = db;
            this.imageService = imageService;
            this.shareStatus = shareStatus;
            this.shareDeleter = shareDeleter;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EntryModel>> Get(string id)
        {
            Ensure.NotNullOrEmpty(id, "id");

            Entry entity = await db.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            Permission permission = Permission.Write;
            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (!await shareStatus.IsEntrySharedForReadAsync(id, userId))
                    return Unauthorized();

                if (!await shareStatus.IsEntrySharedForWriteAsync(id, userId))
                    permission = Permission.Read;
            }

            EntryModel model = new EntryModel();
            MapEntityToModel(entity, model);

            Response.Headers.Add(PermissionHeader.Name, permission.ToString());

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EntryModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

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
        public async Task<ActionResult<EntryModel>> Update(string id, EntryModel model)
        {
            if (id != model.Id)
                return BadRequest();

            Entry entity = await db.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (!await shareStatus.IsEntrySharedForWriteAsync(id, userId))
                    return Unauthorized();
            }

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

            if (entity.UserId != userId)
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
            model.UserId = entity.UserId;
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
