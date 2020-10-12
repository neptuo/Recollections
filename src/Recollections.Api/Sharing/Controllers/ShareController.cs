using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Sharing.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShareController : EntryControllerBase
    {
        private const string publicUserId = "public";

        private readonly DataContext db;
        private readonly IUserNameProvider userNames;

        public ShareController(DataContext db, IUserNameProvider userNames)
            : base(db)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            this.db = db;
            this.userNames = userNames;
        }

        [HttpGet("entries/{entryId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetEntryAsync(string entryId) => RunEntryAsync(entryId, async entry =>
        {
            var items = await db.EntryShares
                .Where(s => s.EntryId == entryId)
                .Select(s => new ShareModel()
                {
                    UserName = s.UserId,
                    Permission = (Permission)s.Permission
                })
                .ToListAsync();

            if (items.Count > 0)
            {
                var p = items.FirstOrDefault(s => s.UserName == publicUserId);
                if (p != null)
                    items.Remove(p);

                var names = await userNames.GetUserNamesAsync(items.Select(s => s.UserName).ToArray());
                for (int i = 0; i < items.Count; i++)
                    items[i].UserName = names[i];

                if (p != null)
                    items.Insert(0, p);
            }

            return Ok(items);
        });

        [HttpPost("entries/{entryId}/sharing")]
        public Task<IActionResult> CreateEntryAsync(string entryId, ShareModel model) => RunEntryAsync(entryId, async entry =>
        {
            string userId = null;
            if (model.UserName != null)
                userId = (await userNames.GetUserIdsAsync(new[] { model.UserName })).First();
            else
                userId = publicUserId;

            EntryShare entity = await db.EntryShares
                .Where(s => s.EntryId == entryId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (entity == null)
            {
                entity = new EntryShare()
                {
                    EntryId = entryId,
                    UserId = userId,
                    Permission = (int)model.Permission
                };

                await db.EntryShares.AddAsync(entity);
            }
            else
            {
                entity.Permission = (int)model.Permission;
            }

            await db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        });

        [HttpDelete("entries/{entryId}/sharing/{userName}")]
        public Task<IActionResult> DeleteEntryAsync(string entryId, string userName) => RunEntryAsync(entryId, async entry =>
        {
            string userId = null;
            if (userName != null)
                userId = (await userNames.GetUserIdsAsync(new[] { userName })).First();
            else
                userId = publicUserId;

            EntryShare entity = await db.EntryShares
                .Where(s => s.EntryId == entryId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (entity == null)
                return NotFound();

            db.EntryShares.Remove(entity);
            await db.SaveChangesAsync();
            return Ok();
        });
    }
}
