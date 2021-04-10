using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public BeingController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus, ShareDeleter shareDeleter)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(shareDeleter, "shareDeleter");
            this.db = db;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.shareDeleter = shareDeleter;
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(List<BeingListModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<BeingListModel>>> GetList()
        {
            var models = new List<BeingListModel>()
            {
                new BeingListModel()
                {
                    Id = "ae08c8cf-0dc8-4123-8c53-55e0c0982f51",
                    Name = "Ivy",
                    Icon = "crow"
                },
                new BeingListModel()
                {
                    Id = "22c011fd-2051-4ad5-9f73-c20ab01ec763",
                    Name = "Sorin",
                    Icon = "dove"
                },
                new BeingListModel()
                {
                    Id = "77ff59de-4d54-49fd-953e-eaad50bd6727",
                    Name = "Mycroft",
                    Icon = "dog"
                }
            };

            foreach (var model in models)
            {
                if (model.UserId == null)
                {
                    model.UserId = "db643987-f0ed-46ae-ad0e-5d44740393f0";
                    model.UserName = "tester";
                }
            }

            return models;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<BeingModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(string id) => RunBeingAsync(id, Permission.Read, async entity =>
        {
            BeingModel model = new BeingModel()
            {
                Id = "ae08c8cf-0dc8-4123-8c53-55e0c0982f51",
                Name = "Ivy",
                Icon = "crow",
                UserId = "db643987-f0ed-46ae-ad0e-5d44740393f0"
            };

            AuthorizedModel<BeingModel> result = new AuthorizedModel<BeingModel>(model);
            result.OwnerId = "db643987-f0ed-46ae-ad0e-5d44740393f0";
            result.OwnerName = "tester";
            result.UserPermission = Permission.Write;

            return Ok(result);
        });

        [HttpPost]
        public async Task<IActionResult> Create(BeingModel model)
        {
            return CreatedAtAction(nameof(Get), new { id = "ae08c8cf-0dc8-4123-8c53-55e0c0982f51" }, model);
        }

        [HttpPut("{id}")]
        [ProducesDefaultResponseType(typeof(BeingModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Update(string id, BeingModel model) => RunBeingAsync(id, Permission.Write, async entity =>
        {
            return NoContent();
        });

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(string id) => RunBeingAsync(id, async entity =>
        {
            return Ok();
        });
    }
}
