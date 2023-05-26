using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.DTOs.BookingDetails;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingDetailController : ControllerBase
    {
        private BookingDetailServices bookingDetailServices;

        public BookingDetailController(IUnitOfWork work)
        {
            bookingDetailServices = new BookingDetailServices(work);
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
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetBookingDetail(Guid bookingDetailId)
        {
            try
            {
                BookingDetailListItemDto? dto = await bookingDetailServices
                    .GetBookingDetailAsync(bookingDetailId);
                if (dto == null)
                {
                    throw new ApplicationException("Booking Detail không tồn tại!!");
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
