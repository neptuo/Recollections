﻿using Microsoft.AspNetCore.Authorization;
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

namespace Neptuo.Recollection.Accounts.Controllers
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

            return BadRequest();
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
    }
}
