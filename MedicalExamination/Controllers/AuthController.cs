namespace MedicalExamination.Controllers
{
    using MedicalExamination.BizLogic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            // This is a dummy user validation. Replace it with your actual user validation logic.
            var user = ValidateUser(loginRequest.Username, loginRequest.Password);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var token = GenerateJwtToken(user);
            return Ok(new LoginResponse { Token = token });
        }

        private User ValidateUser(string username, string password)
        {
            // Replace this with your actual user validation logic
            if (username == "test" && password == "password")
            {
                return new User { Id = 1, Username = "test" };
            }

            return null;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
