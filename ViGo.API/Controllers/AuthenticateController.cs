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
using ViGo.Utilities;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private UserServices userServices;

        public AuthenticateController(IUnitOfWork work)
        {
            userServices = new UserServices(work);
        }

        /// <summary>
        /// Generates JWT token for user
        /// </summary>
        /// <param name="loginUser">User login information</param>
        /// <returns>
        /// JWT token object { token: "" }
        /// </returns>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Login")]
        [ProducesResponseType(401)]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginUser)
        {
            try
            {
                User user = await userServices.LoginAsync(
                    loginUser.Phone, loginUser.Password);

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
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
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

            }
            catch (ApplicationException ex)
            {
                return StatusCode(400, ex.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }

        /// <summary>
        /// Register new user
        /// </summary>
        /// <param name="registerDto">User register information</param>
        /// <returns>
        /// The newly created user
        /// </returns>
        /// <response code="200">Register successfully</response>
        /// <response code="500">Server error</response>
        /// <response code="400">Some information is invalid</response>
        [HttpPost("Register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            try
            {
                if (User.IsAuthenticated())
                {
                    throw new ApplicationException("Bạn đã đăng nhập vào hệ thống!");
                }

                User user = await userServices.RegisterAsync(registerDto);
                return StatusCode(200, user);
            }
            catch (ApplicationException ex)
            {
                return StatusCode(400, ex.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }
    }
}
