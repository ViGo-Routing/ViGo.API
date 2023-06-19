using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.RouteStations;
using ViGo.Models.Stations;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StationController : ControllerBase
    {
        private StationServices stationServices;

        public StationController(IUnitOfWork work)
        {
            stationServices = new StationServices(work);
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
    }
}
