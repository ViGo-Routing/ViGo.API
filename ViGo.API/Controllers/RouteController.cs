﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Routes;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private RouteServices routeServices;

        private ILogger<RouteController> _logger;

        public RouteController(IUnitOfWork work, ILogger<RouteController> logger)
        {
            routeServices = new RouteServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Create new Route for User
        /// </summary>
        /// <returns>
        /// The newly added route
        /// </returns>
        /// <response code="400">Route information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Create Route successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateRoute(RouteCreateModel dto,
            CancellationToken cancellationToken)
        {
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
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(IPagedEnumerable<RouteViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutesCurrentUser(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] RouteSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<RouteViewModel> dtos = await
                routeServices.GetRoutesAsync(IdentityUtilities.GetCurrentUserId(),
                pagination, sorting, HttpContext,
                cancellationToken);
            return StatusCode(200, dtos);
        }

        /// <summary>
        /// Get Routes information for the a specific user
        /// </summary>
        /// <remarks>Only ADMIN</remarks>
        /// <returns>
        /// List of the user's saved routes
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get List of routes successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("User/{userId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(IPagedEnumerable<RouteViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutes(Guid userId,
            [FromQuery] PaginationParameter pagination,
            [FromQuery] RouteSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<RouteViewModel> dtos = await
                routeServices.GetRoutesAsync(userId,
                pagination, sorting, HttpContext,
                cancellationToken);
            return StatusCode(200, dtos);
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
        [ProducesResponseType(typeof(IPagedEnumerable<RouteViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutes(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] RouteSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<RouteViewModel> dtos = await
                routeServices.GetRoutesAsync(null, pagination, sorting, HttpContext,
                cancellationToken);
            return StatusCode(200, dtos);
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
            RouteViewModel dto = await routeServices.GetRouteAsync(routeId, cancellationToken);
            return StatusCode(200, dto);
        }

        /// <summary>
        /// Update Route's information
        /// </summary>
        /// <remarks>
        /// Route's Status cannot be changed here. Use the ChangeStatus endpoint seperately
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
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRoute(Guid routeId, RouteUpdateModel dto,
            CancellationToken cancellationToken)
        {
            if (!routeId.Equals(dto.Id))
            {
                throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
            }

            Domain.Route updatedRoute = await routeServices.UpdateRouteAsync(dto,
                isCalledFromBooking: false,
                cancellationToken);
            return StatusCode(200, updatedRoute);
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
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangeRouteStatus(Guid routeId, RouteChangeStatusModel dto,
            CancellationToken cancellationToken)
        {
            if (!routeId.Equals(dto.Id))
            {
                throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
            }

            Domain.Route updatedRoute = await routeServices.ChangeRouteStatusAsync(dto.Id, dto.Status, cancellationToken);
            return StatusCode(200, updatedRoute);
        }

        /// <summary>
        /// Delete Route
        /// </summary>
        /// <remarks>
        /// Only ADMIN or the user of the Route can delete.
        /// Only Route that has not been booked can be deleted. Routines that 
        /// are belong to the Route are also deleted.
        /// <br />
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
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        [ProducesResponseType(typeof(Domain.Route), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteRoute(Guid routeId,
            CancellationToken cancellationToken)
        {

            Domain.Route deletedRoute = await routeServices.DeleteRouteAsync(routeId, cancellationToken);
            return StatusCode(200, deletedRoute);
        }
    }
}
