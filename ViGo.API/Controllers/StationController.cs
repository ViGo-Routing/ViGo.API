using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.GoogleMaps;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Stations;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StationController : ControllerBase
    {
        private StationServices stationServices;

        private ILogger<StationController> _logger;

        public StationController(IUnitOfWork work, ILogger<StationController> logger)
        {
            stationServices = new StationServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get Station information for a specific station
        /// </summary>
        /// <returns>
        /// Station's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get station's information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{stationId}")]
        [Authorize]
        [ProducesResponseType(typeof(StationViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetStation(Guid stationId,
            CancellationToken cancellationToken)
        {
            StationViewModel? dto = await stationServices.GetStationAsync(stationId, cancellationToken);
            return StatusCode(200, dto);
        }

        /// <summary>
        /// Get List of Metro stations
        /// </summary>
        /// <returns>
        /// List of stations
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get station list successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Metro")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<StationViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMetroStation(Guid stationId,
            CancellationToken cancellationToken)
        {
            IEnumerable<StationViewModel> models = await stationServices
                .GetMetroStationsAsync(cancellationToken);
            return StatusCode(200, models);
        }

        /// <summary>
        /// Check if a point/station is in ViGo region
        /// </summary>
        /// <returns>
        /// True or False
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Result of checking process</response>
        /// <response code="500">Server error</response>
        [HttpGet("In-Region")]
        [Authorize]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> IsInRegion([FromQuery] GoogleMapPoint googleMapPoint,
            CancellationToken cancellationToken)
        {
            bool result = await stationServices.IsStationInRegionAsync(googleMapPoint, cancellationToken);
            return StatusCode(200, result);
        }

        /// <summary>
        /// Create a new Station
        /// </summary>
        /// <returns>
        /// The newly created station information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Station information</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(Station), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateStation([FromBody] StationCreateModel model,
            CancellationToken cancellationToken)
        {
            Station station = await stationServices.CreateStationAsync(model, cancellationToken);
            return StatusCode(200, station);
        }

        /// <summary>
        /// Get list of Stations 
        /// </summary>
        /// <remarks>
        /// List of Stations will be sorted as Metro stations come first
        /// </remarks>
        /// <returns>
        /// List of Stations
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get List of Stations successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IPagedEnumerable<StationViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetStations([FromQuery] PaginationParameter pagination,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<StationViewModel> models = await stationServices
                .GetStationsAsync(pagination, HttpContext, cancellationToken);

            return StatusCode(200, models);
        }

        /// <summary>
        /// Delete a station
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// Deleted Station
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Delete Station successfully</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{stationId}")]
        [ProducesResponseType(typeof(Station), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteStation(Guid stationId,
            CancellationToken cancellationToken)
        {

            Station station = await stationServices.DeleteStationAsync(stationId, cancellationToken);
            return StatusCode(200, station);
        }

        /// <summary>
        /// Update Station's information
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// The updated station information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Station has been updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{stationId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(Station), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateStation(Guid stationId,
            StationUpdateModel model,
            CancellationToken cancellationToken)
        {
            if (!stationId.Equals(model.Id))
            {
                throw new ApplicationException("Thông tin điểm di chuyển không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
            }

            Station station = await stationServices.UpdateStationAsync(model, cancellationToken);
            return StatusCode(200, station);
        }
    }
}
