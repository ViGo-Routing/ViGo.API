using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.QueryString.Pagination;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities;
using ViGo.Utilities.BackgroundTasks;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingDetailController : ControllerBase
    {
        private BookingDetailServices bookingDetailServices;

        private ILogger<BookingDetailController> _logger;

        private IBackgroundTaskQueue _backgroundQueue;
        private IServiceScopeFactory _serviceScopeFactory;

        public BookingDetailController(IUnitOfWork work,
            ILogger<BookingDetailController> logger,
            IBackgroundTaskQueue backgroundQueue,
            IServiceScopeFactory serviceScopeFactory)
        {
            bookingDetailServices = new BookingDetailServices(work, logger);
            _logger = logger;
            _backgroundQueue = backgroundQueue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Get information of one Booking Detail.
        /// </summary>
        /// <returns>
        /// Booking Detail information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get Booking Detail information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{bookingDetailId}")]
        [ProducesResponseType(typeof(BookingDetailViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookingDetail(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            BookingDetailViewModel? dto = await bookingDetailServices
                .GetBookingDetailAsync(bookingDetailId, cancellationToken);
            if (dto == null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!");
            }

            return StatusCode(200, dto);
        }

        /// <summary>
        /// Get upcoming Trip for a user
        /// </summary>
        /// <remarks>If there is no upcoming trip, the result will be null.</remarks>
        /// <returns>
        /// Upcoming Booking Detail information.
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get Booking Detail information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Upcoming/{userId}")]
        [ProducesResponseType(typeof(BookingDetailViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetUpcomingBookingDetail(Guid userId,
            CancellationToken cancellationToken)
        {
            BookingDetailViewModel? dto = await bookingDetailServices
                .GetUpcomingTripAsync(userId, cancellationToken);
            //if (dto == null)
            //{
            //    throw new ApplicationException("Booking Detail không tồn tại!!");
            //}

            return StatusCode(200, dto);
        }

        /// <summary>
        /// Get current Trip for a user
        /// </summary>
        /// <remarks>If there is no current trip, the result will be null.</remarks>
        /// <returns>
        /// Current Booking Detail information.
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get Booking Detail information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Current/{userId}")]
        [ProducesResponseType(typeof(BookingDetailViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetCurrentBookingDetail(Guid userId,
            CancellationToken cancellationToken)
        {
            BookingDetailViewModel? dto = await bookingDetailServices
                .GetCurrentTripAsync(userId, cancellationToken);
            //if (dto == null)
            //{
            //    throw new ApplicationException("Booking Detail không tồn tại!!");
            //}

            return StatusCode(200, dto);
        }

        /// <summary>
        /// Get list of Booking Details that are assigned to a driver
        /// </summary>
        /// <remarks>If BookingId is provided, only Booking Details which are assigned to the driver 
        /// of that Booking are fetched</remarks>
        /// <returns>
        /// List of Booking Details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of booking details successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Driver/{driverId}")]
        [HttpGet("Driver/{driverId}/{bookingId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<BookingDetailViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> GetDriverAssignedBookingDetails(
            Guid driverId, Guid? bookingId, [FromQuery] PaginationParameter pagination,
            [FromQuery] BookingDetailSortingParameters sorting,
            [FromQuery] BookingDetailFilterParameters filters,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}
            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetUserBookingDetailsAsync(
                    driverId, bookingId, pagination, sorting, filters, HttpContext,
                    cancellationToken);
            return StatusCode(200, dtos);
        }

        /// <summary>
        /// Get driver's schedule for picking a booking detail
        /// </summary>
        /// <remarks>The picking Booking Detail will be the current trip, if the driver has a previous or a next trip, they will be fetched</remarks>
        /// <returns>
        /// Previous and Next trip (if available) for the picking trip
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get schedules successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Driver/PickSchedules/{bookingDetailId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<DriverSchedulesForPickingResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> GetDriverScheduleForPickingBookingDetail(
            Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}
            DriverSchedulesForPickingResponse schedules = await bookingDetailServices
                .GetDriverSchedulesForPickingAsync(bookingDetailId, cancellationToken);
            return StatusCode(200, schedules);
        }

        /// <summary>
        /// Get list of Booking Details that are belong to a customer
        /// </summary>
        /// <returns>
        /// List of Booking Details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of booking details successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Customer/{customerId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<BookingDetailViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        public async Task<IActionResult> GetCustomerBookingDetails(
            Guid customerId, [FromQuery] PaginationParameter pagination,
            [FromQuery] BookingDetailSortingParameters sorting,
            [FromQuery] BookingDetailFilterParameters filters,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}
            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetUserBookingDetailsAsync(
                    customerId, null, pagination, sorting, filters, HttpContext,
                    cancellationToken);
            return StatusCode(200, dtos);
        }


        /// <summary>
        /// Get list of Booking Details that are belong to a Booking
        /// </summary>
        /// <returns>
        /// List of Booking Details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of booking details successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Booking/{bookingId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<BookingDetailViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookingDetails(Guid bookingId,
            [FromQuery] PaginationParameter pagination,
            [FromQuery] BookingDetailSortingParameters sorting,
            [FromQuery] BookingDetailFilterParameters filters,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetBookingDetailsAsync(
                    bookingId,
                    pagination, sorting, filters, HttpContext,
                    cancellationToken);
            return StatusCode(200, dtos);
        }


        /// <summary>
        /// Update Status for Booking Detail.
        /// </summary>
        /// <remarks>
        /// For Status of ARRIVE_AT_PICKUP, GOING, ARRIVE_AT_DROPOFF, a Time property must be provided.
        /// </remarks>
        /// <returns>
        /// The updated Booking Detail
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Update successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("UpdateStatus/{bookingDetailId}")]
        [ProducesResponseType(typeof(BookingDetail), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> UpdateBookingDetailStatus(
            Guid bookingDetailId, BookingDetailUpdateStatusModel dto,
            CancellationToken cancellationToken)
        {
            if (!bookingDetailId.Equals(dto.BookingDetailId))
            {
                throw new ApplicationException("Request không hợp lệ!!");
            }

            (BookingDetail bookingDetail, Guid customerId) = await bookingDetailServices
                .UpdateBookingDetailStatusAsync(dto, cancellationToken);

            if (bookingDetail != null && bookingDetail.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
            {
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.TripWasCompletedHandlerAsync(bookingDetail.Id, customerId, false, token);
                    }
                });
            }
            return StatusCode(200, bookingDetail);
        }

        /// <summary>
        /// Manually assign a driver to a Booking detail
        /// </summary>
        /// <remarks>
        /// Only ADMIN can perform this task
        /// </remarks>
        /// <returns>
        /// The updated Booking Detail
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Invalid role</response>
        /// <response code="200">Assign driver successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Driver/Assign/{bookingDetailId}")]
        [ProducesResponseType(typeof(BookingDetail), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignDriver(
            Guid bookingDetailId, BookingDetailAssignDriverModel dto,
            CancellationToken cancellationToken)
        {
            if (!bookingDetailId.Equals(dto.BookingDetailId))
            {
                throw new ApplicationException("Request không hợp lệ!!");
            }

            BookingDetail bookingDetail = await bookingDetailServices
                .AssignDriverAsync(dto, cancellationToken);

            if (bookingDetail != null &&
                bookingDetail.Status == BookingDetailStatus.ASSIGNED
                && bookingDetail.DriverId.HasValue)
            {
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateTripCancelRateAsync(bookingDetail.DriverId.Value, token);
                    }
                });
            }
            return StatusCode(200, bookingDetail);
        }

        /// <summary>
        /// Driver picks a Booking Detail
        /// </summary>
        /// <remarks>
        /// Only DRIVER can perform this task
        /// </remarks>
        /// <returns>
        /// The updated Booking Detail with Driver information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Invalid role</response>
        /// <response code="200">Driver picks successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Driver/Pick/{bookingDetailId}")]
        [ProducesResponseType(typeof(BookingDetail), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> DriverPicksBookingDetail(
            Guid bookingDetailId,
            CancellationToken cancellationToken)
        {

            BookingDetail bookingDetail = await bookingDetailServices
                .DriverPicksBookingDetailAsync(bookingDetailId, cancellationToken);

            if (bookingDetail != null &&
                bookingDetail.Status == BookingDetailStatus.ASSIGNED
                && bookingDetail.DriverId.HasValue)
            {
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateTripCancelRateAsync(bookingDetail.DriverId.Value, token);
                    }
                });
            }

            return StatusCode(200, bookingDetail);
        }

        /// <summary>
        /// Driver picks a list of Booking Details
        /// </summary>
        /// <remarks>
        /// Only DRIVER can perform this task
        /// </remarks>
        /// <returns>
        /// The list of picked booking detail ids and error message for the un-picked booking details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Invalid role</response>
        /// <response code="200">Driver picks successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Driver/Pick")]
        [ProducesResponseType(typeof(PickBookingDetailsResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> DriverPicksBookingDetails(
            [FromBody] IEnumerable<Guid> bookingDetailIds,
            CancellationToken cancellationToken)
        {

            PickBookingDetailsResponse response = await bookingDetailServices
                .DriverPicksBookingDetailsAsync(bookingDetailIds, cancellationToken);

            if (response.SuccessBookingDetailIds != null &&
                response.SuccessBookingDetailIds.Any())
            {
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateTripCancelRateAsync(response.DriverId, token);
                    }
                });
            }

            return StatusCode(200, response);
        }

        /// <summary>
        /// Calculate driver wage for a Booking Detail
        /// </summary>
        /// <remarks>
        /// Customer cannot perform this action
        /// </remarks>
        /// <returns>
        /// The calculated driver wage
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Invalid role</response>
        /// <response code="200">Wage is calculated successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("DriverWage/{bookingDetailId}")]
        [ProducesResponseType(typeof(double), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,STAFF,DRIVER")]
        public async Task<IActionResult> CalculateDriverWage(
            Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            double driverWage = await bookingDetailServices.CalculateDriverWageAsync(bookingDetailId, cancellationToken);
            return StatusCode(200, driverWage);
        }

        /// <summary>
        /// Calculate driver fee for picking a Booking Detail
        /// </summary>
        /// <remarks>
        /// Customer cannot perform this action
        /// </remarks>
        /// <returns>
        /// The calculated fee
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Invalid role</response>
        /// <response code="200">Fee is calculated successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("DriverFee/{bookingDetailId}")]
        [ProducesResponseType(typeof(double), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> CalculateDriverPickFee(
            Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            double driverFee = await bookingDetailServices.CalculateDriverPickFeeAsync(bookingDetailId, cancellationToken);
            return StatusCode(200, driverFee);
        }

        /// <summary>
        /// Get list of available Booking Details that driver can pick to drive
        /// </summary>
        /// <remarks>Custom Sorting is not applicable.
        /// <br />
        /// If BookingId is provided, only available Booking Details of that Booking are fetched!</remarks>
        /// <returns>
        /// List of available Booking Details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of booking details successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Driver/Available/{driverId}")]
        [HttpGet("Driver/Available/{driverId}/{bookingId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<BookingDetailViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> GetDriverAvailableBookingDetails(
            Guid driverId, Guid? bookingId, [FromQuery] PaginationParameter pagination,
            [FromQuery] BookingDetailFilterParameters filters,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}
            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetDriverAvailableBookingDetailsAsync(
                    driverId, bookingId, pagination, filters, HttpContext,
                    cancellationToken);
            return StatusCode(200, dtos);
        }


        /// <summary>
        /// Cancel a Booking Detail
        /// </summary>
        /// <returns>
        /// The canceled Booking Detail
        /// </returns>
        /// <response code="400">Booking Detail information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Cancel BookingDetail successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("Cancel/{bookingDetailId}")]
        [Authorize]
        [ProducesResponseType(typeof(BookingDetail), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CancelBookingDetail(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            (BookingDetail bookingDetail, Guid? userId, bool isInWeek,
                bool isDriverPaid, Guid? customerId) = await bookingDetailServices
                .CancelBookingDetailAsync(bookingDetailId, cancellationToken);

            if (bookingDetail != null
                && bookingDetail.Status == BookingDetailStatus.CANCELLED
                && userId != null)
            {
                // Calculate Trip canceling rate
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateTripCancelRateAsync(userId.Value, token);

                        if (IdentityUtilities.GetCurrentRole() == UserRole.DRIVER)
                        {
                            // Driver cancels Trip
                            await backgroundServices.SendNotificationForNewTripsAsync(token);
                        }
                    }
                });

                if (isInWeek)
                {
                    // Calculate Weekly Trip canceling rate
                    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                    {
                        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                        {
                            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                            await backgroundServices.CalculateWeeklyTripCancelRateAsync(userId.Value, 1, token);
                        }
                    });
                }

                if (isDriverPaid && customerId.HasValue)
                {
                    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                    {
                        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                        {
                            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                            await backgroundServices.TripWasCompletedHandlerAsync(bookingDetail.Id, customerId.Value, true, token);
                        }
                    });
                }
            }

            return StatusCode(200, bookingDetail);
        }

        /// <summary>
        /// User give feedback on Booking Detail
        /// </summary>
        /// <response code="400">Booking Detail information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Feedback successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("Feedback/{bookingDetailId}")]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(BookingDetailViewModel), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UserUpdateFeedback(Guid bookingDetailId,
            [FromBody] BookingDetailFeedbackModel feedback, CancellationToken cancellationToken)
        {
            (BookingDetailViewModel bookingDetailView, Guid driverId) = await
                 bookingDetailServices.UserUpdateFeedback(bookingDetailId, feedback, cancellationToken);

            if (bookingDetailView != null)
            {
                // Calculate Rating
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CalculateDriverRatingAsync(driverId, token);
                    }
                });
            }

            return StatusCode(200, bookingDetailView);
        }

        /// <summary>
        /// Get Booking Details analysis data.
        /// </summary>
        /// <remarks>If the current user is admin, all booking details in the system will be fetched. 
        /// <br/>
        /// Otherwise, only the booking details of the current customer will be fetched.
        /// </remarks>
        /// <returns>
        /// Booking Details analysis information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get Booking Details analysis successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Analysis")]
        [ProducesResponseType(typeof(BookingDetailAnalysisModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookingDetailAnalysis(
            CancellationToken cancellationToken)
        {
            BookingDetailAnalysisModel analysisModel = await bookingDetailServices
                .GetBookingDetailAnalysisAsync(cancellationToken);

            return StatusCode(200, analysisModel);
        }

    }
}
