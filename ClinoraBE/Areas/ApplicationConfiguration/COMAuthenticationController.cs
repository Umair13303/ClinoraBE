using ClinoraBE.Areas.ApplicationConfiguration.Service;
using ClinoraBE.Model.DAL;
using ClinoraBE.Model.Enum;
using ClinoraBE.Model.Security;
using ClinoraBE.Model.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClinoraBE.Areas.ApplicationConfiguration.Controllers
{
    [Area("ApplicationConfiguration")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class COMAuthenticationController : ControllerBase
    {
        private readonly ClinoraContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICOMAuthentication _authService;

        public COMAuthenticationController(ICOMAuthentication authService,ClinoraContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _authService = authService;
        }

        [HttpPost("UserLogin_Validation")]
        public async Task<IActionResult> post_UserLogin([FromBody] User postedData)
        {
            if (postedData == null || string.IsNullOrWhiteSpace(postedData.Email) || string.IsNullOrWhiteSpace(postedData.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email == postedData.Email && (u.Status == true || u.Status == null) && u.UserTypeId == (int)UserType.WebUser);

            if (user == null || user.Password != postedData.Password)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var expiryMinutes = double.Parse(jwtSettings["ExpiryMinutes"] ?? "600");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim("BranchId", user.BranchId?.ToString() ?? ""),
                    new Claim("CompanyId", user.CompanyId?.ToString() ?? ""),
                    new Claim("UserTypeId", user.UserTypeId.ToString()),
                    new Claim("RoleId", user.RoleId.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new AuthenticationPayLoad
            {
                Token = tokenString,
                UserId = user.Id,
                Email = user.Email,
                Name = user.Description,
                BranchId = user.BranchId,
                CompanyId = user.CompanyId,
                UserTypeId = user.UserTypeId,
                RoleId = user.RoleId
            });
        }

        [HttpGet("UserMenu_Populate")]
        public async Task<ActionResult<List<VMMenu>>> get_UserMenu()
        {
            var menus = await _authService.getVRight_By_RoleId();
            return Ok(menus);
        }
    }
}