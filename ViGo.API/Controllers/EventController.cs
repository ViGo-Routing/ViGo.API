using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Threading;
using ViGo.Models.Events;
using ViGo.Repository.Core;
using ViGo.Models.QueryString.Pagination;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private EventServices eventServices;
        private ILogger<EventController> _logger;

        public EventController(IUnitOfWork work, ILogger<EventController> logger)
        {
            eventServices = new EventServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get list of Events
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list of Events successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<EventViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllEvents(
            [FromQuery] PaginationParameter? pagination, CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<EventViewModel> eventView = await
                eventServices.GetAllEvents(pagination, HttpContext, cancellationToken);
            return StatusCode(200, eventView);
        }

        /// <summary>
        /// Get list of Events has status is ACTIVE
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<EventViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN, CUSTOMER")]
        [HttpGet("GetByStatus")]
        public async Task<IActionResult> GetAllEventsActive(
            [FromQuery] PaginationParameter? pagination, CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<EventViewModel> eventView = await
                eventServices.GetAllEventsActive(pagination, HttpContext, cancellationToken);
            return StatusCode(200, eventView);

        }

        /// <summary>
        /// Get Event by ID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get Event successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(EventViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN, CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetiGetEventByIDd(Guid id, CancellationToken cancellationToken)
        {
            EventViewModel eventView = await eventServices.GetEventByID(id, cancellationToken);
            if (eventView == null)
            {
                throw new ApplicationException("Event ID không tồn tại!");
            }
            return StatusCode(200, eventView);
        }


        /// <summary>
        /// Create New Event
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Created successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(EventViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody]EventCreateModel eventCreate, CancellationToken cancellationToken)
        {
            EventViewModel eventView = await eventServices.CreateEvent(eventCreate, cancellationToken);
            if (eventView == null)
            {
                throw new ApplicationException("Tạo thất bại!");
            }
            return StatusCode(200, eventView);
        }

        /// <summary>
        /// Update Event
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(EventViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody]EventUpdateModel eventUpdate)
        {
            EventViewModel eventView = await eventServices.UpdateEvent(id, eventUpdate);
            return StatusCode(200, eventView);

        }
    }
}
