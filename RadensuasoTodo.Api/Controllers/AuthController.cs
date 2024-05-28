using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RadensuasoTodo.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RadensuasoTodo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMongoDatabase database, JwtSettings jwtSettings, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection = database.GetCollection<User>("Users");
        private readonly JwtSettings _jwtSettings = jwtSettings;
        private readonly ILogger<AuthController> _logger = logger;

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            _logger.LogInformation("Register attempt for user: {Username}", userDto.Username);

            var existingUser = await _usersCollection.Find(u => u.Username == userDto.Username).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                _logger.LogWarning("Username already exists: {Username}", userDto.Username);
                return BadRequest("Username already exists.");
            }

            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password)
            };

            await _usersCollection.InsertOneAsync(user);

            _logger.LogInformation("User registered successfully: {Username}", user.Username);
            return Ok(new { user.Id, user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserDto userDto)
        {
            _logger.LogInformation("Login attempt for user: {Username}", userDto.Username);

            var user = await _usersCollection.Find(u => u.Username == userDto.Username).FirstOrDefaultAsync();
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
                user.Id != null ? new Claim("userId", user.Id.ToString()) : null,
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
