using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.Fares;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.BackgroundTasks;
using ViGo.Utilities.Google.Firebase;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FirebaseController : ControllerBase
    {
        private ILogger<FirebaseController> _logger;
        private IBackgroundTaskQueue _backgroundQueue;
        private IServiceScopeFactory _serviceScopeFactory;

        public FirebaseController(ILogger<FirebaseController> logger, 
            IBackgroundTaskQueue backgroundQueue, 
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _backgroundQueue = backgroundQueue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Send a new message
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Message sent successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Message/{bookingDetailId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> SendMessage(Guid bookingDetailId,
            [FromBody] FirestoreMessage message,
            CancellationToken cancellationToken)
        {
            (Guid receiver, string text) = await FirestoreUtilities.DbInstance.SendMessageAsync(
                bookingDetailId, message, cancellationToken);

            if (!string.IsNullOrEmpty(text))
            {
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);

                        await backgroundServices.SendMessageNotificationAsync(receiver, text, token) ;
                    }
                });
            }
            return StatusCode(200);
        }
    }
}
