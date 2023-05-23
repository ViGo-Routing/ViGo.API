using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.DTOs.Routes;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities;
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
        [Authorize(Roles = "CUSTOMER,DRIVER")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateRoute(RouteCreateEditDto dto)
        {
            try
            {
                Domain.Route route = await routeServices.CreateRouteAsync(dto);

                return StatusCode(200, route);
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
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoutes()
        {
            try
            {
                IEnumerable<RouteListItemDto> dtos = await
                    routeServices.GetRoutesAsync(IdentityUtilities.GetCurrentUserId());
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
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRoute(Guid routeId)
        {
            try
            {
                RouteListItemDto dto = await routeServices.GetRouteAsync(routeId);
                return StatusCode(200, dto);
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

        /// <summary>
        /// Update Route's information for the current user. Route's Status cannot be changed here. Use the ChangeStatus endpoint seperately
        /// </summary>
        /// <returns>
        /// The updated route information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Route has been updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{routeId}")]
        [Authorize(Roles = "CUSTOMER,DRIVER")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRoute(Guid routeId, RouteCreateEditDto dto)
        {
            try
            {
                if (!routeId.Equals(dto.Id))
                {
                    throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
                }

                Domain.Route updatedRoute = await routeServices.UpdateRouteAsync(dto);
                return StatusCode(200, updatedRoute);
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
        [Authorize(Roles = "CUSTOMER,DRIVER")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangeRouteStatus(Guid routeId, RouteChangeStatusDto dto)
        {
            try
            {
                if (!routeId.Equals(dto.Id))
                {
                    throw new ApplicationException("Thông tin tuyến đường không trùng khớp! Vui lòng kiểm tra ID của tuyến đường");
                }

                Domain.Route updatedRoute = await routeServices.ChangeRouteStatusAsync(dto.Id, dto.Status);
                return StatusCode(200, updatedRoute);
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
