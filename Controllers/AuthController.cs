using Auth_JWT.Data;
using Auth_JWT.Models.Auth;
using Auth_JWT.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Auth_JWT.Controllers
{
    [Route("API/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private readonly ApiDbContext context;

        public AuthController(UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiDbContext context)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
            this.context = context;
        }

        [HttpPost("LoginAsync")]
        public async Task<IActionResult> LoginAsync([FromForm]LoginVM model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return BadRequest("Wrong credential.");

            var result = await userManager.CheckPasswordAsync(user, model.Password);

            if (!result)
                return BadRequest("Wrong password.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Noob")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection("AuthSettings:Key").Value));

            var token = new JwtSecurityToken(
                issuer: configuration.GetSection("AuthSettings:Issuer").Value,
                audience: configuration.GetSection("AuthSettings:Audience").Value,
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            string tokenAsString = new JwtSecurityTokenHandler().WriteToken(token);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(30),
                SameSite = SameSiteMode.None,
                Secure = true
            };

            httpContextAccessor.HttpContext?.Response.Cookies.Append("TOKEN_TEST_LOGIN", tokenAsString, cookieOptions);

            return Ok(new
            {
                Message = "Authenticated.",
            });
        }

        [HttpPost("LogoutAsync")]
        public async Task<IActionResult> LogoutAsync()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(-30),
                SameSite = SameSiteMode.None,
                Secure = true
            };

            httpContextAccessor.HttpContext?.Response.Cookies.Append("TOKEN_TEST_LOGIN", "", cookieOptions);

            return Ok(new
            {
                Message = "Unthenticated.",
            });
        }

        [HttpPost("RegisterAsync")]
        public async Task<IActionResult> RegisterAsync([FromForm]RegisterVM model)
        {
            if (!ModelState.IsValid) {
                return BadRequest(model);
            }

            var existUser = context.Users.FirstOrDefault(x => x.Email == model.Email);

            if (existUser is not null)
                return BadRequest("Email already used by another.");

            var user = new IdentityUser { UserName = model.Username, Email = model.Email };
            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(result);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(result.Errors);
        }
    }
}
