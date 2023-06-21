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
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services;
using ViGo.Utilities;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private UserServices userServices;
        private FirebaseServices firebaseServices;

        private ILogger<UserController> _logger;

        public UserController(IUnitOfWork work, ILogger<UserController> logger)
        {
            userServices = new UserServices(work, logger);
            firebaseServices = new FirebaseServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get list of Users
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<UserViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsersAsync(
            [FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }

            IPagedEnumerable<Domain.User> users =
                await userServices.GetUsersAsync(pagination, HttpContext, cancellationToken);
            return StatusCode(200, users);
        }

        /////// <summary>
        /////// Get User information
        /////// </summary>
        /////// <remarks>Authorization required</remarks>
        /////// <returns>User's information</returns>
        //[Authorize]
        //[HttpGet("User/{userId}")]
        //public async Task<IActionResult> GetUserAsync(Guid userId)
        //{
        //    try
        //    {
        //        User user =
        //             await userServices.GetUserByIdAsync(userId);
        //        return StatusCode(200, user);
        //    }
        //    catch (ApplicationException ex)
        //    {
        //        return StatusCode(400, ex.GeneratorErrorMessage());
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.GeneratorErrorMessage());
        //    }
        //}

        //[Authorize]
        //[HttpPost("Generate-Firebase")]
        //public async Task<IActionResult> GenerateFirebaseUsers()
        //{
        //    try
        //    {
        //        await firebaseServices.CreateFirebaseUsersAsync();
        //        return StatusCode(200);
        //    }
        //    catch (ApplicationException ex)
        //    {
        //        return StatusCode(400, ex.GeneratorErrorMessage());
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.GeneratorErrorMessage());
        //    }
        //}

        /// <summary>
        /// Generates Firebase Token for Customer and Driver.
        /// </summary>
        /// <remarks>FOR BACK-END TESTING ONLY</remarks>
        /// <param name="phone">User phone number</param>
        /// <returns>
        /// Firebase token object { token: "" }
        /// </returns>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Generate-Firebase-Token")]
        public async Task<IActionResult> GenerateFirebaseToken(string phone,
            CancellationToken cancellationToken)
        {
            string token = await firebaseServices.GenerateFirebaseToken(phone, cancellationToken);
            return StatusCode(200, new { token = token });
        }


        /// <summary>
        /// Get User by id
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<UserViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid userId)
        {
            User user = await userServices.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException("UserID không tồn tại!");
            }
            return StatusCode(200, user);
        }

        /// <summary>
        /// Update information of User
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<User>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUserAsync(Guid userId, [FromBody] UserUpdateModel userUpdate)
        {
            User user = await userServices.UpdateUserAsync(userId, userUpdate);
            return StatusCode(200, user);
        }

        /// <summary>
        /// Update FCM Token for User
        /// </summary>
        /// <response code="401">Update failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Update successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<User>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpPut("UpdateFcm/{userId}")]
        public async Task<IActionResult> UpdateUserFcmTokenAsync(Guid userId,
            [FromBody] UserUpdateFcmTokenModel model, CancellationToken cancellationToken)
        {
            if (!userId.Equals(model.Id))
            {
                throw new ApplicationException("Thông tin ID người dùng không hợp lệ!!");
            }
            User user = await userServices.UpdateUserFcmToken(model, cancellationToken);

            return StatusCode(200, user);
        }
    }
}
