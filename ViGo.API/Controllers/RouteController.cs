using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.Routes;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private RouteServices routeServices;

        public RouteController(IUnitOfWork work)
        {
            routeServices = new RouteServices(work);
        }

        /// <summary>
        /// Create new Route for User
        /// </summary>
        /// <param name="dto">Route information to be created</param>
        /// <returns>
        /// The newly added route
        /// </returns>
        /// <response code="400">Route information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Create Route successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateRoute(RouteCreateEditModel dto,
            CancellationToken cancellationToken)
        {
            //try
            //{
            //    Domain.Route route = await routeServices.CreateRouteAsync(dto);

            //    return StatusCode(200, route);
            //}
            //catch (AccessDeniedException ex)
            //{
            //    return StatusCode(403, ex.GeneratorErrorMessage());
            //}
            //catch (ApplicationException appEx)
            //{
            //    return StatusCode(400, appEx.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
            Domain.Route route = await routeServices.CreateRouteAsync(dto, cancellationToken);

            return StatusCode(200, route);
        }

        /// <summary>
        /// Get Routes information for the current user
        /// </summary>
        /// <returns>
        /// List of current user's saved routes
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get List of routes successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("CurrentUser")]
        [Authorize(Roles = "CUSTOMER,DRIVER")]
        [ProducesResponseType(typeof(IEnumerable<RouteViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutesCurrentUser(CancellationToken cancellationToken)
        {
            //try
            //{
                IEnumerable<RouteViewModel> dtos = await
                    routeServices.GetRoutesAsync(IdentityUtilities.GetCurrentUserId(), cancellationToken);
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

        /// <summary>
        /// Get Routes information
        /// </summary>
        /// <remarks>ADMIN only</remarks>
        /// <returns>
        /// List of all the saved routes
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get List of routes successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<RouteViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutes(CancellationToken cancellationToken)
        {
            //try
            //{
                IEnumerable<RouteViewModel> dtos = await
                    routeServices.GetRoutesAsync(null, cancellationToken);
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

        /// <summary>
        /// Get Route information for a specific route
        /// </summary>
        /// <returns>
        /// Route's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get route's information successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{routeId}")]
        [Authorize]
        [ProducesResponseType(typeof(RouteViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoute(Guid routeId,
            CancellationToken cancellationToken)
        {
            //try
            //{
                RouteViewModel dto = await routeServices.GetRouteAsync(routeId, cancellationToken);
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
        /// Update Route's information
        /// </summary>
        /// <remarks>
        ///  Route's Status cannot be changed here. Use the ChangeStatus endpoint seperately
        /// </remarks>
        /// <returns>
        /// The updated route information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Route has been updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{routeId}")]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRoute(Guid routeId, RouteCreateEditModel dto,
            CancellationToken cancellationToken)
        {
            //try
            //{
                if (!routeId.Equals(dto.Id))
                {
                    throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
                }

                Domain.Route updatedRoute = await routeServices.UpdateRouteAsync(dto, cancellationToken);
                return StatusCode(200, updatedRoute);
            //}
            //catch (AccessDeniedException ex)
            //{
            //    return StatusCode(403, ex.GeneratorErrorMessage());
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
        /// Update Route Status
        /// </summary>
        /// <returns>
        /// The updated route information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Route has been updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("ChangeStatus/{routeId}")]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangeRouteStatus(Guid routeId, RouteChangeStatusModel dto,
            CancellationToken cancellationToken)
        {
            //try
            //{
                if (!routeId.Equals(dto.Id))
                {
                    throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
                }

                Domain.Route updatedRoute = await routeServices.ChangeRouteStatusAsync(dto.Id, dto.Status, cancellationToken);
                return StatusCode(200, updatedRoute);
            //}
            //catch (AccessDeniedException ex)
            //{
            //    return StatusCode(403, ex.GeneratorErrorMessage());
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
        /// Delete Route
        /// </summary>
        /// <remarks>
        /// Only ADMIN can delete.
        /// Only Route that has EVERY BookingDetails which have not been assigned to any Driver can be deleted.
        /// Soft Delete
        /// </remarks>
        /// <returns>
        /// The deleted route information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Route has been deleted successfully</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{routeId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteRoute(Guid routeId,
            CancellationToken cancellationToken)
        {
            //try
            //{

                Domain.Route deletedRoute = await routeServices.DeleteRouteAsync(routeId, cancellationToken);
                return StatusCode(200, deletedRoute);
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
