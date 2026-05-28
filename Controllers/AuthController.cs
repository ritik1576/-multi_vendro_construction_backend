using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InframartAPI_New.Models;

namespace InframartAPI_New.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // TEMP DB (demo purpose only)
        private static List<User> users = new List<User>();

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //  REGISTER
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (users.Any(x => x.Email == request.Email))
                return BadRequest("Email already exists");

            var user = new User
            {
                Id = users.Count + 1,
                Name = request.Name,
                Email = request.Email,
                Password = request.Password,
                Role = "Vendor"
            };

            users.Add(user);

            return Ok(new
            {
                message = "User registered successfully"
            });
        }

        //  LOGIN
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = users.FirstOrDefault(x =>
                x.Email == request.Email &&
                x.Password == request.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var jwtKey = _configuration["JwtSettings:Key"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        //  PROTECTED API
        [Authorize]
        [HttpGet("secure")]
        public IActionResult Secure()
        {
            return Ok("You are authorized!");
        }
    }

    // DTOs (keep here or move to DTO folder later)
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}