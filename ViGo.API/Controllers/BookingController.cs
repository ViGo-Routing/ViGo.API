using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Bookings;
using ViGo.Models.Fares;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Routes;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities;
using ViGo.Utilities.BackgroundTasks;
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
        private IBackgroundTaskQueue _backgroundQueue;
        private IServiceScopeFactory _serviceScopeFactory;

        public BookingController(IUnitOfWork work,
            ILogger<BookingController> logger,
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundTaskQueue backgroundQueue)
        {
            bookingServices = new BookingServices(work, logger);
            fareServices = new FareServices(work, logger);
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _backgroundQueue = backgroundQueue;
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
        /// Otherwise, only bookings of current user (Customer) will be fetched.
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
        public async Task<IActionResult> GetBookings(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] BookingSortingParameters sorting,
            [FromQuery] BookingFilterParameters filters,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}
            IPagedEnumerable<BookingViewModel> dtos;
            if (IdentityUtilities.IsAdmin())
            {
                // Get All Bookings
                dtos = await bookingServices.GetBookingsAsync(null,
                    pagination, sorting, filters, HttpContext,
                    cancellationToken);
            }
            else
            {
                // Get current user's bookings
                dtos = await bookingServices.GetBookingsAsync(
                    IdentityUtilities.GetCurrentUserId(),
                    pagination, sorting, filters, HttpContext,
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
        [ProducesResponseType(typeof(Booking), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateBooking(BookingCreateModel dto,
            CancellationToken cancellationToken)
        {
            Booking booking = await bookingServices.CreateBookingAsync(dto, cancellationToken);

            if (booking != null)
            {
                // Calculate Trip canceling rate
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateTripCancelRateAsync(booking.CustomerId, token);
                    }
                });
            }

            return StatusCode(200, booking);
        }

        /// <summary>
        /// Cancel a whole Booking
        /// </summary>
        /// <returns>
        /// The canceled Booking
        /// </returns>
        /// <response code="400">Booking information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Cancel Booking successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("Cancel/{bookingId}")]
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        [ProducesResponseType(typeof(Booking), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CancelBooking(Guid bookingId,
            CancellationToken cancellationToken)
        {
            (Booking booking, Guid? customerId, int inWeekCount,
                bool isDriverPaid, IEnumerable<Guid> completedBookingDetailIds) =
                await bookingServices.CancelBookingAsync(bookingId, cancellationToken);

            if (booking != null && booking.Status == BookingStatus.CANCELED_BY_BOOKER
                && customerId != null)
            {
                // Calculate Trip canceling rate
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateTripCancelRateAsync(customerId.Value, token);
                    }
                });

                if (inWeekCount > 0)
                {
                    // Calculate Weekly Trip canceling rate
                    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                    {
                        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                        {
                            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                            await backgroundServices.CalculateWeeklyTripCancelRateAsync(customerId.Value, inWeekCount, token);
                        }
                    });
                }

                if (isDriverPaid && completedBookingDetailIds.Any())
                {
                    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                    {
                        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                        {
                            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                            foreach (Guid bookingDetailId in completedBookingDetailIds)
                            {
                                await backgroundServices.TripWasCompletedHandlerAsync(bookingDetailId, customerId.Value, true, token);
                            }
                        }
                    });
                }
            }
            return StatusCode(200, booking);
        }

        /// <summary>
        /// Get Booking analysis data.
        /// </summary>
        /// <remarks>If the current user is admin, all bookings in the system will be fetched. 
        /// <br/>
        /// Otherwise, only the bookings of the current customer will be fetched.
        /// </remarks>
        /// <returns>
        /// Booking analysis information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get Booking analysis successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Analysis")]
        [ProducesResponseType(typeof(BookingAnalysisModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        public async Task<IActionResult> GetBookingAnalysis(
            CancellationToken cancellationToken)
        {
            BookingAnalysisModel analysisModel = await bookingServices
                .GetBookingAnalysisAsync(cancellationToken);

            return StatusCode(200, analysisModel);
        }

    }
}
