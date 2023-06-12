using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.Routes;
using ViGo.Models.RouteStations;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteStationController : ControllerBase
    {
        private RouteStationServices routeStationServices;

        public RouteStationController(IUnitOfWork work)
        {
            routeStationServices = new RouteStationServices(work);
        }

        /// <summary>
        /// Get RouteStation information for a specific route station
        /// </summary>
        /// <returns>
        /// Route Station's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get route station's information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{routeStationId}")]
        [Authorize]
        [ProducesResponseType(typeof(RouteStationViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRouteStation(Guid routeStationId,
            CancellationToken cancellationToken)
        {
            //try
            //{
                RouteStationViewModel? dto = await routeStationServices.GetRouteStationAsync(routeStationId, cancellationToken);
                return StatusCode(200, dto);
            //}
            //catch (ApplicationException appEx)
            //{
            //    return StatusCode(400, appEx.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
        }

        /// <summary>
        /// Get List of RouteStations information for a specific route
        /// </summary>
        /// <returns>
        /// List of Route Stations
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of route stations information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Route/{routeId}")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<RouteStationViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRouteStations(Guid routeId,
            CancellationToken cancellationToken)
        {
            //try
            //{
                IEnumerable<RouteStationViewModel> dtos = await routeStationServices.
                    GetRouteStationsAsync(routeId, cancellationToken);
                return StatusCode(200, dtos);
            //}
            //catch (ApplicationException appEx)
            //{
            //    return StatusCode(400, appEx.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
        }
    }
}
