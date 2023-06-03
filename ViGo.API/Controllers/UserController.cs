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

        public UserController(IUnitOfWork work)
        {
            userServices = new UserServices(work);
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

        //[Authorize]
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
