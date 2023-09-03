using Microsoft.AspNetCore.Mvc;
using ViGo.Repository.Core;
using ViGo.Utilities.Google;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private ILogger<TestController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MapController(IUnitOfWork work, ILogger<TestController> logger, 
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Get estimated Duration in minutes between 2 points
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
        [HttpPost("Duration")]
        public async Task<IActionResult> GetDurationBetweenTwoPoints(
            [FromBody] DurationRequest durationRequest,
            CancellationToken cancellationToken)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();

            double duration = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
                durationRequest.Origin, durationRequest.Destination, httpClient, cancellationToken);
            return StatusCode(200, duration);
        }

        /// <summary>
        /// Get estimated Distance in kilometers between 2 points
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
    }
}
