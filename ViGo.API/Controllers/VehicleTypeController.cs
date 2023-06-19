using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Vehicles;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleTypeController : ControllerBase
    {
        private VehicleTypeServices vehicleTypeServices;

        public VehicleTypeController(IUnitOfWork work)
        {
            vehicleTypeServices = new VehicleTypeServices(work);
        }

        /// <summary>
        /// Get list of Vehicle Type
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehicleType>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllVehicleTypesAsync()
        {
            //try
            //{
                IEnumerable<Domain.VehicleType> vehicleTypes = await vehicleTypeServices.GetAllVehicleTypesAsync();
                return StatusCode(200, vehicleTypes);
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
        /// Get Vehicle Type by ID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehicleType>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleTypeByIdAsync(Guid id)
        {
            //try
            //{
                VehicleType vehicleType = await vehicleTypeServices.GetVehicleTypeByIdAsync(id);
                if(vehicleType == null)
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
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehicleType>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateVehicleTypeAsync([FromBody] VehicleTypeCreateModel vehicleTypeCreate)
        {
            //try
            //{
                VehicleType vehicleType = await vehicleTypeServices.CreateVehicleTypeAsync(vehicleTypeCreate);
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
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehicleType>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
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
