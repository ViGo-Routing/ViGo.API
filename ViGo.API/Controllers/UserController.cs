﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.Users;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services;

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
        /// <response code="200"> list successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<UserViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsersAsync(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] UserSortingParameters sorting,
            [FromQuery] UserFilterParameters filters,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<Domain.User> users =
                await userServices.GetUsersAsync(pagination, sorting, filters, HttpContext, cancellationToken);
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
        /// <response code="200">Generated Firebase Token successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Generate-Firebase-Token")]
        public async Task<IActionResult> GenerateFirebaseToken(string phone,
            UserRole role,
            CancellationToken cancellationToken)
        {
            string token = await firebaseServices.GenerateFirebaseToken(phone, role, cancellationToken);
            return StatusCode(200, new { token = token });
        }


        /// <summary>
        /// Get User by id
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid userId)
        {
            UserViewModel user = await userServices.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException("UserID không tồn tại!");
            }
            return StatusCode(200, user);
        }

        /// <summary>
        /// Get current logged in user
        /// </summary>
        /// <response code="401">User in unauthorized</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get current user successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet("CurrentUser")]
        public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            UserViewModel? user = await userServices.GetCurrentUserAsync(cancellationToken);

            if (user is null)
            {
                throw new ApplicationException("Người dùng không tồn tại!");
            }

            return StatusCode(200, user);
        }

        /// <summary>
        /// Get Customer of a Booking Detail
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet("BookingDetail/{bookingDetailId}")]
        public async Task<IActionResult> GetBookingDetailCustomerAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            UserViewModel user = await userServices
                .GetBookingDetailCustomerAsync(bookingDetailId, cancellationToken);

            return StatusCode(200, user);
        }

        /// <summary>
        /// Get User by phone number
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet("Phone/{phone}")]
        public async Task<IActionResult> GetUserByPhoneAsync(string phone,
            CancellationToken cancellationToken)
        {
            UserViewModel? user = await userServices.GetUserByPhoneNumberAsync(phone, cancellationToken);

            if (user == null)
            {
                throw new ApplicationException("Người dùng không tồn tại!");
            }

            return StatusCode(200, user);
        }

        /// <summary>
        /// Update information of User
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<User>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUserAsync(Guid userId,
            [FromBody] UserUpdateModel userUpdate)
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

        /// <summary>
        /// Update User Status
        /// </summary>
        /// <response code="401">Update failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpPut("UpdateStatus/{id}")]
        public async Task<IActionResult> ChangeUserStatus(Guid id,
            [FromBody] UserChangeStatusModel statusModel,
            CancellationToken cancellationToken)
        {
            UserViewModel userView = await userServices.ChangeUserStatus(id,
                statusModel, cancellationToken);
            return StatusCode(200, userView);
        }

        /// <summary>
        /// Get User analysis data.
        /// </summary>
        /// <remarks>Admin only
        /// </remarks>
        /// <returns>
        /// User analysis information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get User analysis successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Analysis")]
        [ProducesResponseType(typeof(UserAnalysisModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetUserAnalysis(
            CancellationToken cancellationToken)
        {
            UserAnalysisModel analysisModel = await userServices
                .GetUserAnalysisAsync(cancellationToken);

            return StatusCode(200, analysisModel);
        }

        /// <summary>
        /// Get single User analysis data.
        /// </summary>
        /// <remarks>Driver only for now
        /// </remarks>
        /// <returns>
        /// User analysis information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get User analysis successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Analysis/{userId}")]
        [ProducesResponseType(typeof(SingleUserAnalysisModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER,DRIVER")]
        public async Task<IActionResult> GetUserAnalysis(
            Guid userId,
            CancellationToken cancellationToken)
        {
            SingleUserAnalysisModel? analysisModel = await userServices
                .GetSingleUserAnalysisAsync(userId, cancellationToken);

            return StatusCode(200, analysisModel);
        }

        /// <summary>
        /// Get available drivers for a trip
        /// </summary>
        /// <remarks>Admin only
        /// </remarks>
        /// <returns>
        /// List of available drivers
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of available drivers successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Available/{bookingDetailId}")]
        [ProducesResponseType(typeof(IEnumerable<UserViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAvailableDrivers(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            IEnumerable<UserViewModel> drivers = await userServices.GetAvailableDriversForTrip(bookingDetailId,
                cancellationToken);

            return StatusCode(200, drivers);
        }
    }
}
