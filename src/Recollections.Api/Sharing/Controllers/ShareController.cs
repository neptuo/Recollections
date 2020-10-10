using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShareController : ControllerBase
    {
        private readonly DataContext dataContext;
        private static readonly Dictionary<(string type, string id), List<ShareModel>> storage = new Dictionary<(string type, string id), List<ShareModel>>();

        public ShareController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }

        [HttpGet("entries/{entryId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShareModel>> GetEntryAsync(string entryId)
        {
            Ensure.NotNullOrEmpty(entryId, "entryId");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entity = await dataContext.Entries.FindAsync(entryId);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            var key = ("entry", entryId);
            if (!storage.TryGetValue(key, out var items))
                items = new List<ShareModel>();

            return Ok(items);
        }

        [HttpPost("entries/{entryId}/sharing")]
        public async Task<IActionResult> CreateEntryAsync(string entryId, ShareModel model)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (model.UserName == null)
                model.UserName = "Everyone";

            var key = ("entry", entryId);
            if (!storage.TryGetValue(key, out var items))
                storage[key] = items = new List<ShareModel>();

            if (model.Permission == Permission.Read)
            {
                if (items.Any(s => s.UserName == null))
                    return BadRequest();
            }

            ShareModel existing = items.FirstOrDefault(s => s.UserName == model.UserName);
            if (existing != null)
                existing.Permission = model.Permission;
            else
                items.Add(model);

            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpDelete("entries/{entryId}/sharing/{userName}")]
        public async Task<IActionResult> DeleteEntryAsync(string entryId, string userName)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            var key = ("entry", entryId);
            if (storage.TryGetValue(key, out var items))
            {
                var item = items.FirstOrDefault(s => s.UserName == userName);
                if (item != null)
                {
                    items.Remove(item);
                    return Ok();
                }
            }

            return NotFound();
        }
    }
}
