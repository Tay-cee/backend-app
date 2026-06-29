using backend_app.Models;
using backend_app.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend_app.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(UserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User.RegisterDto model)
        {
            if (_userService.FindUsername(model.Username) != null) 
            {
                return BadRequest(new { message = "Username already exists" });
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = _userService.HashPassword(model.Password)
            };

            _userService.AddUser(user);

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User.LoginDto model)
        {
            var user = _userService.FindUsername(model.Username);
            if (user == null || !_userService.VerifyPassword(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
            var token = GenerateJwtToken(user);
            return Ok(new User.AuthResponseDto(token, DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:ExpirationMinutes"]))));
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var username = User.FindFirstValue(ClaimTypes.Name)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

            if (username == null) return Unauthorized();

            var user = _userService.FindUsername(username);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new User.UserDto(user.Username, user.Email));
        }


        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var taken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpirationMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(taken);
        }
    }
}
