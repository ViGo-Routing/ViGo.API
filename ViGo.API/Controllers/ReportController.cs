using Castle.Core.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Reports;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.BackgroundTasks;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private ReportServices reportServices;
        private ILogger<ReportController> _logger;

        private IBackgroundTaskQueue _backgroundQueue;
        private IServiceScopeFactory _serviceScopeFactory;
        public ReportController(IUnitOfWork work, ILogger<ReportController> logger,
            IBackgroundTaskQueue backgroundQueue,
            IServiceScopeFactory serviceScopeFactory)
        {
            reportServices = new ReportServices(work, logger);
            _logger = logger;
            _backgroundQueue = backgroundQueue;
            _serviceScopeFactory = serviceScopeFactory;
        }


        /// <summary>
        /// Get list of Reports by Admin
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list of Reports successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<ReportViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpGet("Admin")]
        public async Task<IActionResult> GetAllReports(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] ReportSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<ReportViewModel> reportViews = await
                reportServices.GetAllReports(pagination, sorting, HttpContext, cancellationToken);
            return StatusCode(200, reportViews);
        }

        /// <summary>
        /// Get list of Reports of a user by current user id
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<ReportViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER, DRIVER")]
        [HttpGet("User")]
        public async Task<IActionResult> GetAllReportsByUserID(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] ReportSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}
            IPagedEnumerable<ReportViewModel> reportViews = await
                reportServices.GetAllReportsByUserID(pagination, sorting, HttpContext, cancellationToken);
            return StatusCode(200, reportViews);

        }

        /// <summary>
        /// Get Report by ReportID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(ReportViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN, CUSTOMER, DRIVER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(Guid id, CancellationToken cancellationToken)
        {
            ReportViewModel reportView = await reportServices.GetReportById(id, cancellationToken);
            if (reportView is null)
            {
                throw new ApplicationException("Report ID không tồn tại!");
            }
            return StatusCode(200, reportView);
        }


        /// <summary>
        /// Create new Report
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Created successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(ReportViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER, DRIVER")]
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportCreateModel reportCreate, CancellationToken cancellationToken)
        {
            ReportViewModel reportView = await reportServices.CreateReport(reportCreate, cancellationToken);
            if (reportView is null)
            {
                throw new ApplicationException("Tạo mới report không thành công!");
            }
            return StatusCode(200, reportView);
        }

        /// <summary>
        /// User update report
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(ReportViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER, DRIVER")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UserUpdateReport(Guid id, [FromBody] ReportUpdateModel reportUpdate)
        {
            ReportViewModel reportView = await reportServices.UserUpdateReport(id, reportUpdate);
            return StatusCode(200, reportView);
        }

        /// <summary>
        /// Admin update report
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(ReportViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpPut("Review/{reportID}")]
        public async Task<IActionResult> AdminUpdateReport(Guid reportID, [FromBody] ReportAdminUpdateModel reportAdminUpdate,
            CancellationToken cancellationToken)
        {
            (ReportViewModel reportView, Guid? customerId) 
                = await reportServices.AdminUpdateReport(reportID, reportAdminUpdate, cancellationToken);

            if (reportView != null && reportView.BookingDetail != null && customerId.HasValue)
            {
                if (reportView.BookingDetail.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
                {
                    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                    {
                        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                        {
                            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                            await backgroundServices.TripWasCompletedHandlerAsync(reportView.BookingDetail.Id, customerId.Value, false, token);
                        }
                    });
                }
            }
            return StatusCode(200, reportView);
        }
    }
}
