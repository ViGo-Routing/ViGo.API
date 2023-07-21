using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Vehicles;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Models.QueryString.Pagination;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleTypeController : ControllerBase
    {
        private VehicleTypeServices vehicleTypeServices;

        private ILogger<VehicleTypeController> _logger;

        public VehicleTypeController(IUnitOfWork work, ILogger<VehicleTypeController> logger)
        {
            vehicleTypeServices = new VehicleTypeServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get list of Vehicle Type
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list of Vehicle Type successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<VehicleType>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllVehicleTypesAsync(
            [FromQuery] PaginationParameter? pagination, CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }

            IPagedEnumerable<VehicleType> vehicleTypes = await
                vehicleTypeServices.GetAllVehicleTypesAsync(pagination, HttpContext, cancellationToken);
            return StatusCode(200, vehicleTypes);
        }

        /// <summary>
        /// Get Vehicle Type by ID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get Vehicle Type successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(VehicleType), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleTypeByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            //try
            //{
            VehicleType vehicleType = await vehicleTypeServices.GetVehicleTypeByIdAsync(id, cancellationToken);
            if (vehicleType == null)
            {
                throw new ApplicationException("Vehicle Type ID không tồn tại!");
            }
            return StatusCode(200, vehicleType);
            //}
            //catch (ApplicationException ex)
            //{
            //    return StatusCode(400, ex.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
        }

        /// <summary>
        /// Create new Vehicle Type
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Create successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(VehicleType), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles ="ADMIN")]
        [HttpPost]
        public async Task<IActionResult> CreateVehicleTypeAsync([FromBody] VehicleTypeCreateModel vehicleTypeCreate, CancellationToken cancellationToken)
        {
            //try
            //{
            VehicleType vehicleType = await vehicleTypeServices.CreateVehicleTypeAsync(vehicleTypeCreate, cancellationToken);
            if (vehicleType == null)
            {
                throw new ApplicationException("Tạo thất bại!");
            }
            return StatusCode(200, vehicleType);
            //}
            //catch (ApplicationException ex)
            //{
            //    return StatusCode(400, ex.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
        }

        /// <summary>
        /// Update information of Vehicle Type
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(VehicleType), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles ="ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicleTypeAsync(Guid id, [FromBody] VehicleTypeUpdateModel vehicleTypeCreate)
        {
            //try
            //{
            VehicleType vehicleType = await vehicleTypeServices.UpdateVehicleTypeAsync(id, vehicleTypeCreate);
            return StatusCode(200, vehicleType);
            //}
            //catch (ApplicationException ex)
            //{
            //    return StatusCode(400, ex.GeneratorErrorMessage());
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, ex.GeneratorErrorMessage());
            //}
        }
    }
}
