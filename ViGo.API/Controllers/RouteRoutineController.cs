using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.RouteRoutines;
using ViGo.Models.RouteStations;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteRoutineController : ControllerBase
    {
        private RouteRoutineServices routeRoutineServices;

        public RouteRoutineController(IUnitOfWork work)
        {
            routeRoutineServices = new RouteRoutineServices(work);
        }

        /// <summary>
        /// Get list of RouteRoutines for a specific route
        /// </summary>
        /// <returns>
        /// List of Route's Routines
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get list of route's routines successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Route/{routeId}")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<RouteRoutineViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRouteStation(Guid routeId)
        {
            try
            {
                IEnumerable<RouteRoutineViewModel> dtos = await routeRoutineServices.GetRouteRoutinesAsync(routeId);
                return StatusCode(200, dtos);
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
