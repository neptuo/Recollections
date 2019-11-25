using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neptuo;
using Neptuo.Recollections.Entries.Services;
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
        private readonly DataContext dataContext;
        private readonly ImageService imageService;

        public EntryController(DataContext dataContext, ImageService imageService)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(imageService, "imageService");
            this.dataContext = dataContext;
            this.imageService = imageService;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EntryModel>> Get(string id)
        {
            Ensure.NotNullOrEmpty(id, "id");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entity = await dataContext.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            EntryModel model = new EntryModel();
            MapEntityToModel(entity, model);

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

            await dataContext.Entries.AddAsync(entity);
            await dataContext.SaveChangesAsync();

            MapEntityToModel(entity, model);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EntryModel>> Update(string id, EntryModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (id != model.Id)
                return BadRequest();

            Entry entity = await dataContext.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            MapModelToEntity(model, entity);

            dataContext.Entries.Update(entity);
            await dataContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entity = await dataContext.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            await imageService.DeleteAllAsync(entity);
            dataContext.Entries.Remove(entity);
            await dataContext.SaveChangesAsync();

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
