using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ViGo.Domain;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Routes;
using ViGo.Models.Settings;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingController : ControllerBase
    {
        private SettingServices settingServices;

        private ILogger<SettingController> _logger;

        public SettingController(IUnitOfWork work, ILogger<SettingController> logger)
        {
            settingServices = new SettingServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get Settings information
        /// </summary>
        /// <remarks>Only ADMIN</remarks>
        /// <returns>
        /// List of system settings
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Get List of settings successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(IEnumerable<SettingViewModel>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSettings(
            CancellationToken cancellationToken)
        {

            IEnumerable<SettingViewModel> models = await settingServices
                .GetSettingsAsync(cancellationToken);

            return StatusCode(200, models);
        }


        /// <summary>
        /// Update Setting information
        /// </summary>
        /// <remarks>Only ADMIN</remarks>
        /// <returns>
        /// Updated setting
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Update setting successfully</response>
        /// <response code="500">Server error</response>
        [HttpPut("{settingKey}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(Setting), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateSetting(string settingKey,
            SettingUpdateModel model, CancellationToken cancellationToken)
        {
            if (!settingKey.Equals(model.Key))
            {
                throw new ApplicationException("Thông tin mã cấu hình không hợp lệ!!");
            }

            Setting setting = await settingServices.UpdateSettingAsync(model, cancellationToken);

            return StatusCode(200, setting);
        }
    }
}
