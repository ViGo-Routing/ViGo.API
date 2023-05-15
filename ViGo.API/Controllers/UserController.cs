using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ViGo.Domain;
using ViGo.DTOs.Users;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private UserServices userServices;

        public UserController(IUnitOfWork work)
        {
            userServices = new UserServices(work);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                IEnumerable<Domain.User> users =
                    await userServices.GetUsersAsync();
                return StatusCode(200, users);
            } catch (ApplicationException ex)
            {
                return StatusCode(400, ex.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }

        [HttpPost("Login")]
        [ProducesResponseType(401)]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginUser)
        {
            try
            {
                User user = await userServices.Login(
                    loginUser.Email, loginUser.Password);

                if (user == null)
                {
                    return StatusCode(401, "Đăng nhập không thành công!");
                }

                user.Password = "";

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var authSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(ViGoConfiguration.Secret));

                // Token
                var token = new JwtSecurityToken(
                    issuer: ViGoConfiguration.ValidIssuer,
                    audience: ViGoConfiguration.ValidAudience,
                    expires: DateTime.Now.AddHours(2),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(
                        authSigningKey, SecurityAlgorithms.HmacSha256));

                var accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");

                return StatusCode(200, new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token)
                });

            } catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }
    }
}
