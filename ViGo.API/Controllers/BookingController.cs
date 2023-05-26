using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.DTOs.Bookings;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private BookingServices bookingServices;

        public BookingController(IUnitOfWork work)
        {
            bookingServices = new BookingServices(work);
        }

        /// <summary>
        /// Get list of Bookings. 
        /// </summary>
        /// <remarks>
        /// If the current user is Admin, all the bookings will be fetched.
        /// Otherwise, only bookings of current user (Driver or Customer) will be fetched.
        /// </remarks>
        /// <returns>
        /// List of Bookings
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get List of Bookings successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookings()
        {
            try
            {
                IEnumerable<BookingListItemDto> dtos;
                if (IdentityUtilities.IsAdmin())
                {
                    // Get All Bookings
                    dtos = await bookingServices.GetBookingsAsync();
                } else
                {
                    // Get current user's bookings
                    dtos = await bookingServices.GetBookingsAsync(IdentityUtilities.GetCurrentUserId());
                }
                return StatusCode(200, dtos);
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
        /// Get information of one Booking. BookingDetails are also fetched.
        /// </summary>
        /// <returns>
        /// Booking information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get Booking information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{bookingId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBooking(Guid bookingId)
        {
            try
            {
                BookingListItemDto? dto = await bookingServices.GetBookingAsync(bookingId);
                if (dto == null)
                {
                    throw new ApplicationException("Booking không tồn tại!");
                }
                return StatusCode(200, dto);
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

    }
}
