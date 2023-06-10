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

        public UserController(IUnitOfWork work)
        {
            userServices = new UserServices(work);
            firebaseServices = new FirebaseServices(work);
        }

        /// <summary>
        /// Get list of Users
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
        [HttpGet]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetUsersAsync()
        {
            try
            {
                IEnumerable<Domain.User> users =
                    await userServices.GetUsersAsync();
                return StatusCode(200, users);
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

        ///// <summary>
        ///// Get User information
        ///// </summary>
        ///// <remarks>Authorization required</remarks>
        ///// <returns>User's information</returns>
        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserAsync(Guid userId)
        {
            try
            {
               User user =
                    await userServices.GetUserByIdAsync(userId);
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
        public async Task<IActionResult> GenerateFirebaseToken(string phone)
        {
            try
            {
                string token = await firebaseServices.GenerateFirebaseToken(phone);
                return StatusCode(200, new {token = token});
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
        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid id)
        {
            try
            {
                User user = await userServices.GetUserByIdAsync(id);
                if (user == null)
                {
                    throw new ApplicationException("UserID không tồn tại!");
                }
                return StatusCode(200, user);
            }
            catch (ApplicationException appEx)
            {
                return StatusCode(400, appEx.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }

        /// <summary>
        /// Update information of User
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
        [HttpPut("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUserAsync(Guid id, [FromBody] UserUpdateModel userUpdate)
        {
            try
            {
                User user = await userServices.UpdateUserAsync(id, userUpdate);
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
