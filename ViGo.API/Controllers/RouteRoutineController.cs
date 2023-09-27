using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.RouteRoutines;
using ViGo.Repository.Core;
using ViGo.Services;

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
        /// <remarks>Sorting is not applicable. Routines will be sorted by RoutineDate and PickupTime</remarks>
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
        public async Task<IActionResult> GetRouteRoutines(Guid routeId,
            [FromQuery] PaginationParameter pagination,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IEnumerable<RouteRoutineViewModel> dtos = await routeRoutineServices
                .GetRouteRoutinesAsync(routeId,
                pagination, HttpContext,
                cancellationToken);
            return StatusCode(200, dtos);
        }

        /// <summary>
        /// Get information for a RouteRoutine
        /// </summary>
        /// <returns>
        /// Routine information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get routine's information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{routineId}")]
        [Authorize]
        [ProducesResponseType(typeof(RouteRoutineViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutine(Guid routineId,
            CancellationToken cancellationToken)
        {
            RouteRoutineViewModel routine = await routeRoutineServices.GetRouteRoutineAsync(routineId,
                cancellationToken);
            return StatusCode(200, routine);
        }

        /// <summary>
        /// Create new Route's Routines for user for a specific route.
        /// </summary>
        /// <remarks>
        /// The route should not have Routines already configured. 
        /// Configured Routines should use Update endpoint
        /// </remarks>
        /// <returns>
        /// The newly added routines
        /// </returns>
        /// <response code="400">Routines information are not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Create Routines successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<RouteRoutine>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateRouteRoutines(RouteRoutineCreateModel model,
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
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<RouteRoutine>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRouteRoutines(Guid routeId, RouteRoutineUpdateModel model,
            CancellationToken cancellationToken)
        {
            if (!routeId.Equals(model.RouteId))
            {
                throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
            }

            IEnumerable<RouteRoutine> updatedRoutines = await
                routeRoutineServices.UpdateRouteRoutinesAsync(model, true, cancellationToken);
            return StatusCode(200, updatedRoutines);
        }

        /// <summary>
        /// Update information for a single Routine
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// The updated routine information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Routine has been updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{routineId}")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(RouteRoutine), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRouteRoutine(Guid routineId,
            RouteRoutineSingleUpdateModel model,
            CancellationToken cancellationToken)
        {
            if (!routineId.Equals(model.Id))
            {
                throw new ApplicationException("Thông tin lịch trình không trùng khớp! Vui lòng kiểm tra ID của lịch trình");
            }

            RouteRoutine routeRoutine = await routeRoutineServices.UpdateRouteRoutineAsync(model,
                cancellationToken);

            return StatusCode(200, routeRoutine);
        }

        /// <summary>
        /// Check for valid routines for a route
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// 200 if routines are valid, if not a 400 status is returned
        /// </returns>
        /// <response code="400">Routines information are not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Routines are valid</response>
        /// <response code="500">Server error</response>
        [HttpPost("Validate")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ValidateRouteRoutines(RouteRoutineCheckModel model,
            CancellationToken cancellationToken)
        {
            await routeRoutineServices.CheckRouteRoutinesAsync(model, cancellationToken);

            return StatusCode(200);
        }

        /// <summary>
        /// Check for valid routines for a round trip route
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// 200 if routines are valid, if not a 400 status is returned
        /// </returns>
        /// <response code="400">Routines information are not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Routines are valid</response>
        /// <response code="500">Server error</response>
        [HttpPost("Validate/RoundTrip")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ValidateRoundRouteRoutines(RoundRouteRoutineCheckModel model,
            CancellationToken cancellationToken)
        {
            await routeRoutineServices.CheckRoundRouteRoutinesAsync(model, cancellationToken);

            return StatusCode(200);
        }

        /// <summary>
        /// Delete a Route Routine
        /// </summary>
        /// <remarks>
        /// Only ADMIN or the user of the Routine can delete.
        /// Only Routine that has not been booked can be deleted. Routines that 
        /// are belong to the RoundTrip Route are also deleted.
        /// <br />
        /// Soft Delete
        /// </remarks>
        /// <returns>
        /// The deleted routine information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Routine has been deleted successfully</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{routineId}")]
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteRoutine(Guid routineId,
            CancellationToken cancellationToken)
        {

            RouteRoutine deletedRoutine = await routeRoutineServices.DeleteRouteRoutineAsync(
                routineId, cancellationToken);
            return StatusCode(200, deletedRoutine);
        }
    }
}
