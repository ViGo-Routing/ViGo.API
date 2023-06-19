﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Users;
using ViGo.Models.Vehicles;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private VehicleServices vehicleServices;

        public VehiclesController(IUnitOfWork work)
        {
            vehicleServices = new VehicleServices(work);
        }

        /// <summary>
        /// Get list of Vehicles
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllVehiclesAsync()
        {
            //try
            //{
                IEnumerable<VehiclesViewModel> vehicles = await vehicleServices.GetAllVehiclesAsync();
                return StatusCode(200, vehicles);
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
        /// Get Vehicle by ID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleByIdAsync(Guid id)
        {
            //try
            //{
                VehiclesViewModel vehicle = await vehicleServices.GetVehicleByIdAsync(id);
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
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetVehicleByUserIdAsync(Guid userId)
        {
            IEnumerable<VehiclesViewModel> vehicle = await vehicleServices.GetVehicleByUserIdAsync(userId);
            return StatusCode(200, vehicle);
        }

        /// <summary>
        /// Create new Vehicle
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateVehicleAsync([FromBody] VehiclesCreateModel vehicle)
        {
            //try
            //{
                VehiclesViewModel vehi = await vehicleServices.CreateVehicleAsync(vehicle);
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

        /// <summary>
        /// Update information of Vehicle
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Login successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<VehiclesViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        //[Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicleAsync(Guid id, [FromBody] VehiclesUpdateModel vehiclesUpdate)
        {
            //try
            //{
                VehiclesViewModel vehicle = await vehicleServices.UpdateVehicleAsync(id, vehiclesUpdate);
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
    }
}
