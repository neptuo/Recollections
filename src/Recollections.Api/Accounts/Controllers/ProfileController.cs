using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    [Route("api/profiles")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<User> userManager;

        public ProfileController(UserManager<User> userManager)
        {
            Ensure.NotNull(userManager, "userManager");
            this.userManager = userManager;
        }

        [HttpGet("{id}")]
        [ProducesDefaultResponseType(typeof(UserInfoResponse))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync(string id)
        {
            Ensure.NotNullOrEmpty(id, "id");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            User user = await userManager.FindByIdAsync(id);

            return Ok(new UserInfoResponse()
            {
                UserId = user.Id,
                UserName = user.UserName
            });
        }
    }
}
