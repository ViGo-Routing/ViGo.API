using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.BookingDetails;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingDetailController : ControllerBase
    {
        private BookingDetailServices bookingDetailServices;

        private ILogger<BookingDetailController> _logger;

        public BookingDetailController(IUnitOfWork work, ILogger<BookingDetailController> logger)
        {
            bookingDetailServices = new BookingDetailServices(work, logger);
            _logger = logger;
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
                throw new ApplicationException("Booking Detail không tồn tại!!");
            }

            return StatusCode(200, dto);
        }

        /// <summary>
        /// Get list of Booking Details that are assigned to a driver
        /// </summary>
        /// <returns>
        /// List of Booking Details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of booking details successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Driver/{driverId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<BookingDetailViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> GetDriverAssignedBookingDetails(
            Guid driverId, [FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetDriverAssignedBookingDetailsAsync(
                    driverId, pagination, HttpContext,
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
            [FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }

            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetBookingDetailsAsync(
                    bookingId, 
                    pagination, HttpContext,
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

            BookingDetail bookingDetail = await bookingDetailServices
                .UpdateBookingDetailStatusAsync(dto, cancellationToken);
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
            return StatusCode(200, bookingDetail);
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
        /// Get list of available Booking Details that driver can pick to drive
        /// </summary>
        /// <returns>
        /// List of available Booking Details
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of booking details successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Driver/Available/{driverId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<BookingDetailViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> GetDriverAvailableBookingDetails(
            Guid driverId, [FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<BookingDetailViewModel> dtos =
                await bookingDetailServices.GetDriverAvailableBookingDetailsAsync(
                    driverId, pagination, HttpContext,
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
            BookingDetail bookingDetail = await bookingDetailServices
                .CancelBookingDetailAsync(bookingDetailId, cancellationToken);

            return StatusCode(200, bookingDetail);
        }
    }
}
