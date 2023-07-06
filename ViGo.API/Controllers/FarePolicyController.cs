using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ViGo.Domain;
using ViGo.Models.FarePolicies;
using ViGo.Models.Fares;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Services.Core;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarePolicyController : ControllerBase
    {
        private FarePolicyServices farePolicyServices;
        private ILogger<FarePolicyController> _logger;

        public FarePolicyController(IUnitOfWork work,
            ILogger<FarePolicyController> logger)
        {
            farePolicyServices = new FarePolicyServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Update FarePolicy's information
        /// </summary>
        /// <remarks>ADMIN only and can only update PricePerKm
        /// <br />
        /// If you want to update other properties other than PricePerKm, you must use the 
        /// update Fare endpoint and update all other policies that belong to the Fare as well.
        /// </remarks>
        /// <returns>
        /// The updated fare policy information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">FarePolicy is updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{farePolicyId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(FarePolicy), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateFarePolicy(Guid farePolicyId,
            FarePolicyUpdateModel model,
            CancellationToken cancellationToken)
        {
            if (!farePolicyId.Equals(model.Id))
            {
                throw new ApplicationException("Thông tin ID chính sách giá không hợp lệ!!");
            }

            FarePolicy farePolicy = await
                farePolicyServices.UpdateFarePolicyAsync(model,
                cancellationToken);
            return StatusCode(200, farePolicy);
        }

        /// <summary>
        /// Get list of fare policies of a fare
        /// </summary>
        /// <remarks>ADMIN only
        /// </remarks>
        /// <returns>
        /// The list of fare policies of that fare
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">List of fare policies are fetched successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Fare/{fareId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<FarePolicyViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFarePolicies(Guid fareId,
            CancellationToken cancellationToken)
        {
            IEnumerable<FarePolicyViewModel> models = await farePolicyServices
                .GetFarePoliciesAsync(fareId, cancellationToken);
            return StatusCode(200, models);
        }
    }
}
