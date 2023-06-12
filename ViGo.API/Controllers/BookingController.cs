using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.Bookings;
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
        private FareServices fareServices;

        public BookingController(IUnitOfWork work)
        {
            bookingServices = new BookingServices(work);
            fareServices = new FareServices(work);
        }

        [HttpGet("FareCalculate")]
        public async Task<IActionResult> FareCalculate(double distance)
        {
            //try
            //{
                double tripFare = await fareServices.TestCalculateTripFare(distance);
                return StatusCode(200, tripFare);
            //}
            //catch (ApplicationException appEx)
            //{
            //    return StatusCode(400, appEx.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
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
        [ProducesResponseType(typeof(IEnumerable<BookingViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookings(CancellationToken cancellationToken)
        {
            //try
            //{
                IEnumerable<BookingViewModel> dtos;
                if (IdentityUtilities.IsAdmin())
                {
                    // Get All Bookings
                    dtos = await bookingServices.GetBookingsAsync(null, cancellationToken);
                } else
                {
                    // Get current user's bookings
                    dtos = await bookingServices.GetBookingsAsync(IdentityUtilities.GetCurrentUserId(),
                        cancellationToken);
                }
                return StatusCode(200, dtos);
            //}
            //catch (ApplicationException appEx)
            //{
            //    return StatusCode(400, appEx.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
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
        [ProducesResponseType(typeof(BookingViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBooking(Guid bookingId,
            CancellationToken cancellationToken)
        {
            //try
            //{
                BookingViewModel? dto = await bookingServices.GetBookingAsync(bookingId, cancellationToken);
                if (dto == null)
                {
                    throw new ApplicationException("Booking không tồn tại!");
                }
                return StatusCode(200, dto);
            //}
            //catch (ApplicationException appEx)
            //{
            //    return StatusCode(400, appEx.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
        }

    }
}
