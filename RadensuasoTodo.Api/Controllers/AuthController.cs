using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadensuasoTodo.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RadensuasoTodo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TodoContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(TodoContext context, JwtSettings jwtSettings, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            _logger.LogInformation("Register attempt for user: {Username}", userDto.Username);

            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
            {
                _logger.LogWarning("Username already exists: {Username}", userDto.Username);
                return BadRequest("Username already exists.");
            }

            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Username}", user.Username);
            return Ok(new { user.Id, user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserDto userDto)
        {
            _logger.LogInformation("Login attempt for user: {Username}", userDto.Username);

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == userDto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid username or password for user: {Username}", userDto.Username);
                return Unauthorized("Invalid username or password.");
            }

            var token = GenerateJwtToken(user);

            _logger.LogInformation("User logged in successfully: {Username}", user.Username);
            return Ok(new { user.Id, user.Username, Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds);

            _logger.LogInformation("JWT token generated for user: {Username}", user.Username);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
