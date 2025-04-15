using Boardly.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Boardly.Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult>SignIn()
        {
            return Ok();
        }

        [HttpPost("SignUp")]
        public async Task<IActionResult>SignUp([FromBody] SignIn model)
        {
            if (model.Username == "admin" && model.Password == "admin")
            {
                var claims = new[]
        {
            new Claim(ClaimTypes.Name, model.Username),
            new Claim(ClaimTypes.Role, "Admin"), 
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds
                );

                var tokena = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(new { Token = tokena });
            }

            return Unauthorized("Invalid credentials");
        }

        [HttpPost]
        public async Task<IActionResult>Refresh()
        {
            return Ok();
        }
    }
}
