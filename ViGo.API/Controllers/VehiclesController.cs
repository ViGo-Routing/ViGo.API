using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Vehicles;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private VehicleServices vehicleServices;

        private ILogger<VehiclesController> _logger;

        public VehiclesController(IUnitOfWork work, ILogger<VehiclesController> logger)
        {
            vehicleServices = new VehicleServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get list of Vehicles
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list of Vehicles successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllVehiclesAsync(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] VehicleSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<VehiclesViewModel> vehicles = await
                vehicleServices.GetAllVehiclesAsync(pagination, sorting, HttpContext, cancellationToken);

            return StatusCode(200, vehicles);
        }

        /// <summary>
        /// Get Vehicle by ID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200"> Get Vehicle successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(VehiclesViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            //try
            //{
            VehiclesViewModel vehicle = await vehicleServices.GetVehicleByIdAsync(id, cancellationToken);
            if (vehicle == null)
            {
                throw new ApplicationException("VehicleID không tồn tại!");
            }
            return StatusCode(200, vehicle);
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
        /// Get Vehicle by User ID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200"> Get Vehicle successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetVehicleByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            IEnumerable<VehiclesViewModel> vehicle = await vehicleServices.GetVehicleByUserIdAsync(userId, cancellationToken);
            return StatusCode(200, vehicle);
        }

        /// <summary>
        /// Create new Vehicle
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Created successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(VehiclesViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,DRIVER")]
        [HttpPost]
        public async Task<IActionResult> CreateVehicleAsync([FromBody] VehiclesCreateModel vehicle, CancellationToken cancellationToken)
        {
            //try
            //{
            VehiclesViewModel vehi = await vehicleServices.CreateVehicleAsync(vehicle, cancellationToken);
            if (vehi == null)
            {
                throw new ApplicationException("Tạo thất bại!");
            }
            return StatusCode(200, vehi);
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

        //    /// <summary>
        //    /// Update information of Vehicle
        //    /// </summary>
        //    /// <response code="401">Login failed</response>
        //    /// <response code="400">Some information is invalid</response>
        //    /// <response code="200">Login successfully</response>
        //    /// <response code="500">Server error</response>
        //    [ProducesResponseType(typeof(VehiclesViewModel), 200)]
        //    [ProducesResponseType(401)]
        //    [ProducesResponseType(400)]
        //    [ProducesResponseType(500)]
        //    //[Authorize]
        //    [HttpPut("{id}")]
        //    public async Task<IActionResult> UpdateVehicleAsync(Guid id, [FromBody] VehiclesUpdateModel vehiclesUpdate)
        //    {
        //        //try
        //        //{
        //        VehiclesViewModel vehicle = await vehicleServices.UpdateVehicleAsync(id, vehiclesUpdate);
        //        return StatusCode(200, vehicle);
        //        //}
        //        //catch (ApplicationException appEx)
        //        //{
        //        //    return StatusCode(400, appEx.GeneratorErrorMessage());
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    return StatusCode(500, ex.GeneratorErrorMessage());
        //        //}
        //    }
    }
}
