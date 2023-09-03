using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using ViGo.Domain;
using ViGo.Models.CronJobs;
using ViGo.Models.GoogleMaps;
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
        private CronJobServices cronJobServices;
        private ISchedulerFactory _schedulerFactory;
        private ILogger<TestController> _logger;
        private IHttpClientFactory _httpClientFactory;

        public TestController(IUnitOfWork work, ILogger<TestController> logger,
            ISchedulerFactory schedulerFactory, IHttpClientFactory httpClientFactory)
        {
            bookingServices = new BookingServices(work, logger);
            tripMappingServices = new TripMappingServices(work, logger);
            firebaseServices = new FirebaseServices(work, logger);
            cronJobServices = new CronJobServices(work, logger);
            _schedulerFactory = schedulerFactory;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
        /// <returns></returns>
        [HttpPost("Distance")]
        public async Task<IActionResult> GetDistanceBetweenTwoPoints(
            [FromBody] DistanceRequest distanceRequest,
            CancellationToken cancellationToken)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();

            double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                distanceRequest.Origin, distanceRequest.Destination, httpClient, cancellationToken);
            return StatusCode(200, distance);
        }

        //[HttpGet("TripMapping")]
        //public async Task<IActionResult> TestTripMapping(Guid bookingId,
        //    CancellationToken cancellationToken)
        //{
        //    BookingViewModel bookingModel = await bookingServices.GetBookingAsync(bookingId, cancellationToken);
        //    Booking booking = new Booking()
        //    {
        //        Id = bookingModel.Id,
        //        CustomerRouteId = bookingModel.CustomerRoute.Id,
        //        CustomerId = bookingModel.Customer.Id,
        //        StartTime = bookingModel.StartTime,
        //        StartDate = bookingModel.StartDate,
        //        EndDate = bookingModel.EndDate,
        //        TotalPrice = bookingModel.TotalPrice,
        //        PriceAfterDiscount = bookingModel.PriceAfterDiscount,
        //        PaymentMethod = bookingModel.PaymentMethod,
        //        IsShared = bookingModel.IsShared,
        //        Duration = bookingModel.Duration,
        //        Distance = bookingModel.Distance,
        //        PromotionId = bookingModel.PromotionId,
        //        VehicleTypeId = bookingModel.VehicleTypeId
        //    };

        //    await tripMappingServices.MapBooking(booking, _logger);

        //    return StatusCode(200);
        //}

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

        [Authorize(Roles = "ADMIN")]
        [HttpGet("CronJobs")]
        public async Task<IActionResult> GetCronJobs(CancellationToken cancellationToken)
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();
            IEnumerable<CronJobViewModel> cronJobs = await cronJobServices
                .GetCronJobsAsync(scheduler, cancellationToken);
            return StatusCode(200, cronJobs);
        }
    }



    public class DistanceRequest
    {
        public GoogleMapPoint Origin { get; set; }
        public GoogleMapPoint Destination { get; set; }
    }

    public class DurationRequest
    {
        public GoogleMapPoint Origin { get; set; }
        public GoogleMapPoint Destination { get; set; }
    }
}
