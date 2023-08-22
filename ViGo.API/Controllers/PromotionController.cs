using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Promotions;
using ViGo.Models.QueryString.Pagination;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private PromotionServices promotionServices;
        private ILogger<PromotionController> _logger;

        public PromotionController(IUnitOfWork work,
            ILogger<PromotionController> logger)
        {
            promotionServices = new PromotionServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get List of promotions
        /// </summary>
        /// <remarks>
        /// Only Admin/Staff can get all the information.
        /// </remarks>
        /// <returns>
        /// List of promotions
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">List of promotions are fetched successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IPagedEnumerable<PromotionViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetPromotionsAsync(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] PromotionSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<PromotionViewModel> models = await promotionServices
                .GetPromotionsAsync(null, pagination, sorting, HttpContext,
                    cancellationToken);
            return StatusCode(200, models);
        }

        /// <summary>
        /// Get List of promotions by Event
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <returns>
        /// List of promotions
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">List of promotions are fetched successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Event/{eventId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<PromotionViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetPromotionsAsync(
            Guid eventId,
            [FromQuery] PaginationParameter pagination,
            [FromQuery] PromotionSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<PromotionViewModel> models = await promotionServices
                .GetPromotionsAsync(eventId, pagination, sorting, HttpContext,
                    cancellationToken);
            return StatusCode(200, models);
        }

        /// <summary>
        /// Get detail of a promotion
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <returns>
        /// Promotion information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Promotion details are fetched successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{promotionId}")]
        [ProducesResponseType(typeof(PromotionViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetPromotionAsync(
            Guid promotionId,
            CancellationToken cancellationToken)
        {
            PromotionViewModel model = await promotionServices
                .GetPromotionAsync(promotionId,
                    cancellationToken);
            return StatusCode(200, model);
        }

        /// <summary>
        /// Create new promotion
        /// </summary>
        /// <remarks>
        /// Only Admin/Staff can create
        /// </remarks>
        /// <returns>
        /// Newly created promotion
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Promotion is created successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(Promotion), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreatePromotionAsync(
            PromotionCreateModel model,
            CancellationToken cancellationToken)
        {
            Promotion promotion = await promotionServices
                .CreatePromotionAsync(model,
                    cancellationToken);
            return StatusCode(200, promotion);
        }

        /// <summary>
        /// Update promotion information
        /// </summary>
        /// <remarks>
        /// Only Admin can update. Promotion Code cannot be updated
        /// </remarks>
        /// <returns>
        /// Updated promotion information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Promotion is updated successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{promotionId}")]
        [ProducesResponseType(typeof(Promotion), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdatePromotionAsync(Guid promotionId,
            PromotionUpdateModel model,
            CancellationToken cancellationToken)
        {
            if (!promotionId.Equals(model.Id))
            {
                throw new ApplicationException("Thông tin ID của mã giảm giá không hợp lệ!!!");
            }

            Promotion promotion = await promotionServices
                .UpdatePromotionAsync(model, cancellationToken);
            return StatusCode(200, promotion);
        }

        /// <summary>
        /// Delete a Promotion
        /// </summary>
        /// <remarks>
        /// Only Admin can delete notification
        /// </remarks>
        /// <returns>
        /// Deleted Notification's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Notification is deleted successfully</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{promotionId}")]
        [ProducesResponseType(typeof(IEnumerable<Promotion>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeletePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
        {
            Promotion promotion = await promotionServices.DeletePromotionAsync(promotionId, cancellationToken);
            return StatusCode(200, promotion);
        }
    }
}
