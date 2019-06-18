using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Controllers
{
    [Route("api/accounts/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly JwtOptions configuration;
        private readonly JwtSecurityTokenHandler tokenHandler;

        public AccountController(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> configuration, JwtSecurityTokenHandler tokenHandler)
        {
            Ensure.NotNull(userManager, "userManager");
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(tokenHandler, "tokenHandler");
            this.userManager = userManager;
            this.configuration = configuration.Value;
            this.tokenHandler = tokenHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            ApplicationUser user = await userManager.FindByNameAsync(request.UserName);
            if (user != null)
            {
                if (await userManager.CheckPasswordAsync(user, request.Password))
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    };

                    var credentials = new SigningCredentials(configuration.GetSecurityKey(), SecurityAlgorithms.HmacSha256);
                    var expiry = DateTime.Now.Add(configuration.GetExpiry());

                    var token = new JwtSecurityToken(
                        configuration.Issuer,
                        configuration.Issuer,
                        claims,
                        expires: expiry,
                        signingCredentials: credentials
                    );

                    return Ok(new LoginResponse(tokenHandler.WriteToken(token)));
                }
            }

            return Ok(new LoginResponse());
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var user = new ApplicationUser(model.UserName);
            var result = await userManager.CreateAsync(user, model.Password);

            var response = new RegisterResponse();
            if (!result.Succeeded)
                response.ErrorMessages.AddRange(result.Errors.Select(e => e.Description));

            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Info()
        {
            return Ok(new UserInfoResponse()
            {
                username = HttpContext.User.Identity.Name
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Detail()
        {
            ApplicationUser user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            if (user == null)
                return NotFound();

            return Ok(new UserDetailResponse()
            {
                UserName = user.UserName,
                Created = user.Created
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            ApplicationUser user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            if (user == null)
                return NotFound();

            IdentityResult result = await userManager.ChangePasswordAsync(user, request.Current, request.New);

            var response = new ChangePasswordResponse();
            if (!result.Succeeded)
                response.ErrorMessages.AddRange(result.Errors.Select(e => e.Description));

            return Ok(response);
        }
    }
}
