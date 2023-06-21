using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Bookings;
using ViGo.Models.Fares;
using ViGo.Models.Routes;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
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
        //private TripMappingServices tripMappingServices;

        private ILogger<BookingController> _logger;

        public BookingController(IUnitOfWork work, ILogger<BookingController> logger)
        {
            bookingServices = new BookingServices(work, logger);
            fareServices = new FareServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Calculate Fare for a specific setting of booking
        /// </summary>
        /// <remarks>
        /// All properties are required, except for RoutineType for now.
        /// </remarks>
        /// <returns>
        /// Calculated Fare
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Calculate fare successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("FareCalculate")]
        [ProducesResponseType(typeof(FareCalculateResponseModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> FareCalculate([FromBody] FareCalculateRequestModel model,
            CancellationToken cancellationToken)
        {
            FareCalculateResponseModel response = await
                fareServices.CalculateFareBasedOnDistance(model, cancellationToken);
            return StatusCode(200, response);
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
        [ProducesResponseType(typeof(IPagedEnumerable<BookingViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookings([FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<BookingViewModel> dtos;
            if (IdentityUtilities.IsAdmin())
            {
                // Get All Bookings
                dtos = await bookingServices.GetBookingsAsync(null, 
                    pagination, HttpContext,
                    cancellationToken);
            }
            else
            {
                // Get current user's bookings
                dtos = await bookingServices.GetBookingsAsync(
                    IdentityUtilities.GetCurrentUserId(),
                    pagination, HttpContext,
                    cancellationToken);
            }
            return StatusCode(200, dtos);
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
            BookingViewModel? dto = await bookingServices.GetBookingAsync(bookingId, cancellationToken);
            if (dto == null)
            {
                throw new ApplicationException("Booking không tồn tại!");
            }
            return StatusCode(200, dto);
        }

        /// <summary>
        /// Create new Booking for User's Route and Routine
        /// </summary>
        /// <param name="dto">Booking information to be created</param>
        /// <returns>
        /// The newly added booking
        /// </returns>
        /// <response code="400">Booking information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Create Booking successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Booking), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateBooking(BookingCreateModel dto,
            CancellationToken cancellationToken)
        {
            Booking booking = await bookingServices.CreateBookingAsync(dto, cancellationToken);

            //if (booking != null)
            //{
            //    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
            //    {
            //        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
            //        {
            //            IUnitOfWork work = new UnitOfWork(scope.ServiceProvider);
            //            TripMappingServices tripMappingServices = new TripMappingServices(work);
            //            await tripMappingServices.MapBooking(booking, _logger);
            //        }
            //    });
            //}

            return StatusCode(200, booking);
        }

    }
}
