using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts.Controllers
{
    [Route("api/accounts/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly JwtOptions configuration;
        private readonly JwtSecurityTokenHandler tokenHandler;

        public AccountController(IOptions<JwtOptions> configuration, JwtSecurityTokenHandler tokenHandler)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(tokenHandler, "tokenHandler");
            this.configuration = configuration.Value;
            this.tokenHandler = tokenHandler;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request.Username == "demo" && request.Password == "demo")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.NameIdentifier, 1.ToString())
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

            return BadRequest();
        }
    }
}
