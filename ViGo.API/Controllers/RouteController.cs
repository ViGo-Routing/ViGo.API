using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.DTOs.Routes;
using ViGo.Repository.Core;
using ViGo.Services;
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

        [HttpPost]
        [Authorize(Roles = "CUSTOMER,DRIVER")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateRoute(CreateRouteDto dto)
        {
            try
            {
                Domain.Route route = await routeServices.CreateRouteAsync(dto);

                return StatusCode(200, route);
            } catch (ApplicationException appEx)
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
