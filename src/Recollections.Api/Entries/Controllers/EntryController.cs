using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public EntryController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
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
        }

        private void MapModelToEntity(EntryModel model, Entry entity)
        {
            entity.Id = model.Id;
            entity.Title = model.Title;
            entity.When = model.When;
            entity.Text = model.Text;
        }
    }
}
