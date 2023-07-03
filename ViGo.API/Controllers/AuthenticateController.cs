using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ViGo.Domain;
using ViGo.Models.Users;
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
        private ILogger<AuthenticateController> _logger;

        public AuthenticateController(IUnitOfWork work, ILogger<AuthenticateController> logger)
        {
            _logger = logger;
            userServices = new UserServices(work, logger);
        }

        /// <summary>
        /// Generates JWT token for Admin and Staff
        /// </summary>
        /// <remarks>Login with email and password</remarks>
        /// <returns>
        /// JWT token object { token: "" }
        /// </returns>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Web/Login")]
        [ProducesResponseType(401)]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [AllowAnonymous]
        public async Task<IActionResult> WebLogin([FromBody] WebUserLoginModel loginUser,
            CancellationToken cancellationToken)
        {
            User? user = await userServices.LoginAsync(
                loginUser, cancellationToken);

            if (user == null)
            {
                return StatusCode(401, "Đăng nhập không thành công!");
            }

            if (user.Status == Domain.Enumerations.UserStatus.BANNED)
            {
                return StatusCode(401, "Tài khoản đã bị khóa!");
            }
            if (user.Status == Domain.Enumerations.UserStatus.INACTIVE)
            {
                return StatusCode(401, "Tài khoản đang bị ngưng hoạt động!");
            }

            user.Password = "";

            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? ""),
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
                expires: DateTime.Now.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(
                    authSigningKey, SecurityAlgorithms.HmacSha256));

            var accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");

            return StatusCode(200, new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = user
            });
        }

        /// <summary>
        /// Generates JWT token for Customer and Driver
        /// </summary>
        /// <remarks>Login with phone number and Firebase Token</remarks>
        /// <returns>
        /// JWT token object { token: "" }
        /// </returns>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Mobile/Login")]
        [ProducesResponseType(401)]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [AllowAnonymous]
        public async Task<IActionResult> MobileLogin([FromBody] MobileUserLoginModel loginUser,
            CancellationToken cancellationToken)
        {
            User? user = await userServices.LoginAsync(
                loginUser, cancellationToken);

            if (user == null)
            {
                return StatusCode(401, "Đăng nhập không thành công!");
            }
            if (user.Status == Domain.Enumerations.UserStatus.BANNED)
            {
                return StatusCode(401, "Tài khoản đã bị khóa!");
            }
            if (user.Status == Domain.Enumerations.UserStatus.INACTIVE)
            {
                return StatusCode(401, "Tài khoản đang bị ngưng hoạt động!");
            }

            user.Password = "";

            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? ""),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim("FirebaseUid", user.FirebaseUid ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

            var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(ViGoConfiguration.Secret));

            // Token
            var token = new JwtSecurityToken(
                issuer: ViGoConfiguration.ValidIssuer,
                audience: ViGoConfiguration.ValidAudience,
                expires: DateTime.Now.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(
                    authSigningKey, SecurityAlgorithms.HmacSha256));

            var accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");

            return StatusCode(200, new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = user
            });
        }

        /// <summary>
        /// Register new user
        /// </summary>
        /// <returns>
        /// The newly created user
        /// </returns>
        /// <response code="200">Register successfully</response>
        /// <response code="500">Server error</response>
        /// <response code="400">Some information is invalid</response>
        [HttpPost("Register")]
        [ProducesResponseType(200, Type = typeof(User))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel registerDto,
            CancellationToken cancellationToken)
        {
            if (User.IsAuthenticated())
            {
                throw new ApplicationException("Bạn đã đăng nhập vào hệ thống!");
            }

            User user = await userServices.RegisterAsync(registerDto, cancellationToken);
            return StatusCode(200, user);
        }
    }
}
