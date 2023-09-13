using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Neptuo;
using Neptuo.Events;
using Neptuo.Recollections.Accounts.Events;
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
    [ApiController]
    [Route("api/accounts")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly JwtOptions jwtOptions;
        private readonly TokenLoginOptions tokenOptions;
        private readonly JwtSecurityTokenHandler tokenHandler;

        public AccountController(UserManager<User> userManager, IOptions<JwtOptions> jwtOptions, IOptions<TokenLoginOptions> tokenOptions, JwtSecurityTokenHandler tokenHandler)
        {
            Ensure.NotNull(userManager, "userManager");
            Ensure.NotNull(jwtOptions, "jwtOptions");
            Ensure.NotNull(tokenOptions, "tokenOptions");
            Ensure.NotNull(tokenHandler, "tokenHandler");
            this.userManager = userManager;
            this.jwtOptions = jwtOptions.Value;
            this.tokenOptions = tokenOptions.Value;
            this.tokenHandler = tokenHandler;
        }

        private IActionResult CreateJwtToken(User user, bool isReadOnly = false)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            claims.IsReadOnly(isReadOnly);

            var credentials = new SigningCredentials(jwtOptions.GetSecurityKey(), SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.Add(jwtOptions.GetExpiry());

            var token = new JwtSecurityToken(
                jwtOptions.Issuer,
                jwtOptions.Issuer,
                claims,
                expires: expiry,
                signingCredentials: credentials
            );

            return Ok(new LoginResponse(tokenHandler.WriteToken(token)));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            User user = await userManager.FindByNameAsync(request.UserName);
            if (user != null)
            {
                if (await userManager.CheckPasswordAsync(user, request.Password))
                    return CreateJwtToken(user);
            }

            return Ok(new LoginResponse());
        }

        [HttpPost("login/token")]
        public async Task<IActionResult> LoginWithToken(LoginWithTokenRequest request)
        {
            Ensure.NotNull(request, "request");
            if (tokenOptions.Tokens.TryGetValue(request.Token, out var userName))
            {
                var user = await userManager.FindByNameAsync(userName);
                if (user != null)
                    return CreateJwtToken(user, true);
            }

            return NotFound();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromServices] IEventDispatcher events, RegisterRequest model)
        {
            var user = new User(model.UserName);
            var result = await userManager.CreateAsync(user, model.Password);

            var response = new RegisterResponse();
            if (!result.Succeeded)
                response.ErrorMessages.AddRange(result.Errors.Select(e => e.Description));
            else
                await events.PublishAsync(new UserRegistered(user.Id));

            return Ok(response);
        }

        [Authorize]
        [HttpPost("changepassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            User user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);
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
