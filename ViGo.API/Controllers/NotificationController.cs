using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.Fares;
using ViGo.Models.Notifications;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private NotificationServices notificationServices;
        private ILogger<NotificationController> _logger;

        public NotificationController(IUnitOfWork work, ILogger<NotificationController> logger)
        {
            notificationServices = new NotificationServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get information of a specific notification
        /// </summary>
        /// <remarks>
        /// Admin/Staff can retrieve all the information. Customer/Driver can only retrieve information of 
        /// notifcation that belongs to them
        /// </remarks>
        /// <returns>
        /// Notification's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Notification is retrieved successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("{notificationId}")]
        [ProducesResponseType(typeof(IEnumerable<NotificationViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
        {
            NotificationViewModel model = await notificationServices.GetNotificationAsync(notificationId, cancellationToken);
            return StatusCode(200, model);
        }

        /// <summary>
        /// Get List of notifications that are belong to a specific user
        /// </summary>
        /// <remarks>
        /// Admin/Staff can get all the information. Customer/Driver can only get list of their own notifcations
        /// </remarks>
        /// <returns>
        /// List of user's notifications
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">List of notifications are fetched successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("User/{userId}")]
        [ProducesResponseType(typeof(IPagedEnumerable<NotificationViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> GetUserNotificationsAsync(Guid userId,
            [FromQuery] PaginationParameter? pagination,
            CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }

            IPagedEnumerable<NotificationViewModel> models = await notificationServices
                .GetNotificationsAsync(userId, 
                pagination, HttpContext,
                cancellationToken);
            return StatusCode(200, models);
        }

        /// <summary>
        /// Create new Notification
        /// </summary>
        /// <remarks>
        /// Only Admin/Staff can create notification through this endpoint.
        /// IsSentToUser property: set to true to send the Push Notification to user
        /// </remarks>
        /// <returns>
        /// Newly created Notification's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Notification is created successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<Notification>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> CreateNotificationAsync(NotificationCreateModel model, CancellationToken cancellationToken)
        {
            Notification notification = await notificationServices.CreateNotificationAsync(model, cancellationToken);
            return StatusCode(200, notification);
        }

        /// <summary>
        /// Delete a Notification
        /// </summary>
        /// <remarks>
        /// Only Admin/Staff can delete notification through this endpoint.
        /// </remarks>
        /// <returns>
        /// Deleted Notification's information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Notification is deleted successfully</response>
        /// <response code="500">Server error</response>
        [HttpDelete("{notificationId}")]
        [ProducesResponseType(typeof(IEnumerable<Notification>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
        {
            Notification notification = await notificationServices.DeleteNotificationAsync(notificationId, cancellationToken);
            return StatusCode(200, notification);
        }
    }
}
