using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.UserLicenses;
using ViGo.Repository.Core;
using ViGo.Models.QueryString.Pagination;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLicenseController : ControllerBase
    {
        private UserLicenseServices userLicenseServices;
        private ILogger<UserLicenseController> _logger;

        public UserLicenseController(IUnitOfWork work, ILogger<UserLicenseController> logger)
        {
            userLicenseServices = new UserLicenseServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get list of User Licenses
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get all successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<UserLicenseViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllUserLicenses(
            [FromQuery] PaginationParameter pagination,
            [FromQuery] UserLicenseSortingParameters sorting,
            CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<UserLicenseViewModel> userLicenseViews = await 
                userLicenseServices.GetAllUserLicenses(pagination, sorting,
                HttpContext, cancellationToken);

            return StatusCode(200, userLicenseViews);
        }

        /// <summary>
        /// Get User License by Id
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200"> User License successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserLicenseViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN, DRIVER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserLicenseByID(Guid id, CancellationToken cancellationToken)
        {
            UserLicenseViewModel userLicenseView = await userLicenseServices.GetUserLicenseByID(id, cancellationToken);
            if (userLicenseView is null)
            {
                throw new ApplicationException("User License ID không tồn tại!");
            }
            return StatusCode(200, userLicenseView);
        }


        /// <summary>
        /// Get All User Licenses by UserId
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get all successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IEnumerable<UserLicenseViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN, DRIVER")]
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetAllUserLicensesByUserID(Guid userId, CancellationToken cancellationToken)
        {
            IEnumerable<UserLicenseViewModel> userLicenseView = await userLicenseServices.GetAllUserLicensesByUserID(userId, cancellationToken);
            if (userLicenseView is null)
            {
                throw new ApplicationException("User ID không tồn tại!");
            }
            return StatusCode(200, userLicenseView);
        }


        /// <summary>
        /// Create new User License
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Created successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserLicenseViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles ="DRIVER")]
        [HttpPost]
        public async Task<IActionResult> CreateUserLicense([FromBody] UserLicenseCreateModel userLicenseCreate, CancellationToken cancellationToken)
        {
            UserLicenseViewModel userLicense = await userLicenseServices.CreateUserLicense(userLicenseCreate, cancellationToken);
            if(userLicense == null)
            {
                throw new ApplicationException("Tạo mới không thành công!");
            }
            return StatusCode(200, userLicense);
        }

        /// <summary>
        /// Update User License's status
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(UserLicenseViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserLicense(Guid id, 
            [FromBody] UserLicenseUpdateModel userLicenseUpdateModel,
            CancellationToken cancellationToken)
        {
            UserLicenseViewModel userLicense = await userLicenseServices
                .UpdateUserLicense(id, userLicenseUpdateModel, cancellationToken);
            return StatusCode(200, userLicense);
        }
    }
}
