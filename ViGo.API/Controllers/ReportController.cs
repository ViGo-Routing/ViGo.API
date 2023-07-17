using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Models.Reports;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private ReportServices reportServices;
        private ILogger<ReportController> _logger;
        public ReportController(IUnitOfWork work, ILogger<ReportController> logger)
        {
            reportServices = new ReportServices(work, logger);
            _logger = logger;
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
        public async Task<IActionResult> GetAllReports([FromQuery] PaginationParameter? pagination, CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }

            IPagedEnumerable<ReportViewModel> reportViews = await
                reportServices.GetAllReports(pagination, HttpContext, cancellationToken);
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
        public async Task<IActionResult> GetAllReportsByUserID([FromQuery] PaginationParameter? pagination, CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<ReportViewModel> reportViews = await
                reportServices.GetAllReportsByUserID(pagination, HttpContext, cancellationToken);
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
        [HttpPut("Status&Note/{reportID}")]
        public async Task<IActionResult> AdminUpdateReport(Guid reportID, [FromBody] ReportAdminUpdateModel reportAdminUpdate)
        {
            ReportViewModel reportView = await reportServices.AdminUpdateReport(reportID, reportAdminUpdate);
            return StatusCode(200, reportView);
        }
    }
}
