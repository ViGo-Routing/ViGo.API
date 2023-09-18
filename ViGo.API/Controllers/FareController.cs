using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Fares;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FareController : ControllerBase
    {
        private FareServices fareServices;

        private ILogger<FareController> _logger;

        public FareController(IUnitOfWork work,
            ILogger<FareController> logger)
        {
            fareServices = new FareServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Create new Fare with Fare Policies configured
        /// </summary>
        /// <remarks>Only ADMIN</remarks>
        /// <returns>
        /// The newly added Fare and Fare Policies
        /// </returns>
        /// <response code="400">Fare information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Create Fare successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(FareViewModel), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateFare(FareCreateModel model,
            CancellationToken cancellationToken)
        {
            FareViewModel fareViewModel = await fareServices.CreateFareAsync(model, cancellationToken);

            return StatusCode(200, fareViewModel);
        }

        /// <summary>
        /// Get Fares information along with FarePolicies
        /// </summary>
        /// <remarks>ADMIN only. No pagination required</remarks>
        /// <returns>
        /// List of all the fare and corresponding fare policies
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get List of fares successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        [ProducesResponseType(typeof(IEnumerable<FareViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFares(
            //[FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IEnumerable<FareViewModel> models = await
                fareServices.GetFaresAsync(/*pagination, */HttpContext,
                cancellationToken);
            return StatusCode(200, models);
        }

        /// <summary>
        /// Get a single Fare information along with FarePolicies
        /// </summary>
        /// <remarks>ADMIN only</remarks>
        /// <returns>
        /// The fare information and corresponding fare policies
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get fare successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{fareId}")]
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        [ProducesResponseType(typeof(FareViewModel), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFare(Guid fareId,
            CancellationToken cancellationToken)
        {

            FareViewModel model = await
                 fareServices.GetFareAsync(fareId,
                 cancellationToken);
            return StatusCode(200, model);
        }

        /// <summary>
        /// Get a single Fare information along with FarePolicies for a Vehicle Type
        /// </summary>
        /// <remarks>ADMIN only</remarks>
        /// <returns>
        /// The fare information and corresponding fare policies
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get fare successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("VehicleType/{vehicleTypeId}")]
        [Authorize(Roles = "ADMIN,CUSTOMER")]
        [ProducesResponseType(typeof(FareViewModel), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetVehicleTypeFare(Guid vehicleTypeId,
            CancellationToken cancellationToken)
        {
            FareViewModel model = await
                fareServices.GetVehicleTypeFareAsync(vehicleTypeId,
                cancellationToken);
            return StatusCode(200, model);
        }

        /// <summary>
        /// Update Fare's information
        /// </summary>
        /// <remarks>ADMIN only.
        /// <br />
        /// If you want to update BaseDistance, you must have FarePolicies configured with the request body object as 
        /// BaseDistance and FarePolicies are connected to each other.
        /// </remarks>
        /// <returns>
        /// The updated fare information and corresponding fare policies
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Fare is updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{fareId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(FareViewModel), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateFare(Guid fareId,
            FareUpdateModel model,
            CancellationToken cancellationToken)
        {
            if (!fareId.Equals(model.Id))
            {
                throw new ApplicationException("Thông tin ID cấu hình giá không hợp lệ!!");
            }

            FareViewModel fare = await
                fareServices.UpdateFareAsync(model,
                cancellationToken);
            return StatusCode(200, fare);
        }

        /// <summary>
        /// Delete Fare's information
        /// </summary>
        /// <remarks>ADMIN only.
        /// <br />
        /// Fare Policies are deleted as well.
        /// </remarks>
        /// <returns>
        /// The deleted fare
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Fare is deleted successfully</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{fareId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(Fare), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteFare(Guid fareId,
            CancellationToken cancellationToken)
        {
            Fare fare = await fareServices.DeleteFareAsync(fareId,
                cancellationToken);

            return StatusCode(200, fare);
        }
    }
}
