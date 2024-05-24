namespace MedicalExamination.Controllers
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.Data;
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
        private readonly IPasswordHasher<DboSecret> _passwordHasher;
        private readonly ExaminationDbContext _context;

        public AuthController(IConfiguration configuration, ExaminationDbContext context)
        {
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<DboSecret>();
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginReq loginRequest)
        {
            var user = ValidateUser(loginRequest.Username, loginRequest.Password);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var token = GenerateJwtToken(user);
            return Ok(new LoginResponse { Token = token });
        }

        private User? ValidateUser(string username, string password)
        {
            var user = _context.DboSecrets.SingleOrDefault(d => d.Username == username);
            if (user == null)
            {
                return null;
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password ?? string.Empty, password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }
            return new User { Id = user.Id, Username = user.Username, UserType = (UserType)user.UserType};  
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
