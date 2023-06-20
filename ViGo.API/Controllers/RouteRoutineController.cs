using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Routes;
using ViGo.Models.RouteStations;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteRoutineController : ControllerBase
    {
        private RouteRoutineServices routeRoutineServices;

        private ILogger<RouteRoutineController> _logger;
        public RouteRoutineController(IUnitOfWork work, ILogger<RouteRoutineController> logger)
        {
            routeRoutineServices = new RouteRoutineServices(work, logger);
            _logger = logger;
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
        public async Task<IActionResult> GetRouteStation(Guid routeId,
            CancellationToken cancellationToken)
        {

            IEnumerable<RouteRoutineViewModel> dtos = await routeRoutineServices.GetRouteRoutinesAsync(routeId, cancellationToken);
            return StatusCode(200, dtos);
        }

        /// <summary>
        /// Create new Route's Routines for user for a specific route.
        /// </summary>
        /// <remarks>
        /// The route should not have Routines already configured. 
        /// Configured Routines should use Update endpoint
        /// </remarks>
        /// <param name="model">Routines information to be created</param>
        /// <returns>
        /// The newly added routines
        /// </returns>
        /// <response code="400">Routines information are not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Create Routines successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<RouteRoutine>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateRouteRoutines(RouteRoutineCreateEditModel model,
            CancellationToken cancellationToken)
        {
            IEnumerable<RouteRoutine> routines = await routeRoutineServices.CreateRouteRoutinesAsync(model, cancellationToken);

            return StatusCode(200, routines);
        }

        /// <summary>
        /// Update Route's Routines information for a specific route
        /// </summary>
        /// <remarks>
        /// The route should have Routines already configured. 
        /// Create Routines first if Route has not been configured.
        /// </remarks>
        /// <returns>
        /// The updated routines information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Route Routines have been updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("Route/{routeId}")]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<RouteRoutine>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRouteRoutines(Guid routeId, RouteRoutineCreateEditModel model,
            CancellationToken cancellationToken)
        {
            if (!routeId.Equals(model.RouteId))
            {
                throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
            }

            IEnumerable<RouteRoutine> updatedRoutines = await routeRoutineServices.UpdateRouteRoutinesAsync(model, cancellationToken);
            return StatusCode(200, updatedRoutines);
        }
    }
}
