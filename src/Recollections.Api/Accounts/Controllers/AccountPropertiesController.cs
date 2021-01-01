using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Controllers
{
    [ApiController]
    [Route("api/accounts/properties")]
    public class AccountPropertiesController : ControllerBase
    {
        private readonly DataContext db;
        private readonly UserPropertyOptions options;

        public AccountPropertiesController(DataContext db, IOptions<UserPropertyOptions> options)
        {
            Ensure.NotNull(db, "db");
            this.db = db;
            this.options = options.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetListAsync()
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            List<UserPropertyValue> values = await db.UserProperties
                .Where(p => p.UserId == userId)
                .ToListAsync();

            List<UserPropertyModel> result = new List<UserPropertyModel>();
            foreach (string key in options.Keys)
            {
                result.Add(new UserPropertyModel()
                {
                    Key = key,
                    Value = values.FirstOrDefault(p => p.Key == key)?.Value
                });
            }

            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> PutAsync(UserPropertyValue model)
        {
            Ensure.NotNull(model, "model");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            if (!options.Keys.Contains(model.Value))
                return BadRequest();

            UserPropertyValue entity = await db.UserProperties.FirstOrDefaultAsync(p => p.Key == model.Key && p.UserId == userId);
            if (entity == null)
            {
                if (model.Value != null)
                {
                    entity = new UserPropertyValue()
                    {
                        UserId = userId,
                        Key = model.Key,
                        Value = model.Value
                    };
                    db.UserProperties.Add(entity);
                }
            }
            else if (model.Value == null)
            {
                db.UserProperties.Remove(entity);
            }
            else
            {
                entity.Value = model.Value;
            }

            if (entity != null)
                await db.SaveChangesAsync();

            return Ok();
        }
    }
}
