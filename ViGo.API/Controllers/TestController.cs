using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Bookings;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private BookingServices bookingServices;
        private TripMappingServices tripMappingServices;
        private FirebaseServices firebaseServices;
        private ILogger<TestController> _logger;

        public TestController(IUnitOfWork work, ILogger<TestController> logger)
        {
            bookingServices = new BookingServices(work);
            tripMappingServices = new TripMappingServices(work);
            firebaseServices = new FirebaseServices(work);
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
            /// Sample:
            /// {
            ///  "origin": {
            ///    "latitude": 10.8047903620383,
            ///    "longtitude": 106.79502630962487
            ///  },
            ///  "destination": {
            ///    "latitude": 10.758846315012603,
            ///    "longtitude": 106.67546265195486
            ///  }
            ///}
        /// </remarks>
        /// <param name="distanceRequest"></param>
        /// <returns></returns>
        [HttpPost("Distance")]
        public async Task<IActionResult> GetDistanceBetweenTwoPoints([FromBody] DistanceRequest distanceRequest)
        {
            double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                distanceRequest.Origin, distanceRequest.Destination);
            return StatusCode(200, distance);
        }

        [HttpGet("TripMapping")]
        public async Task<IActionResult> TestTripMapping(Guid bookingId,
            CancellationToken cancellationToken)
        {
            BookingViewModel bookingModel = await bookingServices.GetBookingAsync(bookingId, cancellationToken);
            Booking booking = new Booking()
            {
                Id = bookingModel.Id,
                CustomerRouteId = bookingModel.CustomerRoute.Id,
                CustomerId = bookingModel.Customer.Id,
                StartTime = bookingModel.StartTime,
                StartDate = bookingModel.StartDate,
                EndDate = bookingModel.EndDate,
                TotalPrice = bookingModel.TotalPrice,
                PriceAfterDiscount = bookingModel.PriceAfterDiscount,
                PaymentMethod = bookingModel.PaymentMethod,
                IsShared = bookingModel.IsShared,
                Duration = bookingModel.Duration,
                Distance = bookingModel.Distance,
                PromotionId = bookingModel.PromotionId,
                VehicleTypeId = bookingModel.VehicleTypeId
            };

            await tripMappingServices.MapBooking(booking, _logger);

            return StatusCode(200);
        }

        [Authorize]
        [HttpPost("Generate-Firebase")]
        public async Task<IActionResult> GenerateFirebaseUsers(string phone, CancellationToken cancellationToken)
        {
            try
            {
                string uid = await firebaseServices.CreateFirebaseUserAsync(phone, cancellationToken);
                return StatusCode(200, new { Id = uid });
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



    public class DistanceRequest
    {
        public GoogleMapPoint Origin { get; set; }
        public GoogleMapPoint Destination { get; set; }
    }
}
