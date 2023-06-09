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

        ///// <summary>
        ///// Get List of Users
        ///// </summary>
        ///// <remarks>Authorization required</remarks>
        ///// <returns>List of current users</returns>
        //[Authorize]
        [HttpGet]
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

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUserAsync(Guid id, UserUpdateModel userUpdate)
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
