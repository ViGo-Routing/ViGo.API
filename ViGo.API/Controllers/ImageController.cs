using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ViGo.Models.Routes;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.ImageUtilities;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private ILogger<ImageController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ImageController(ILogger<ImageController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Read text from image. For Identification and Driver license document
        /// </summary>
        /// <returns>
        /// The text read from image
        /// </returns>
        /// <response code="400">Image information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">User Role is not valid</response>
        /// <response code="200">Read from image successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("ReadText")]
        [Authorize]
        [ProducesResponseType(typeof(ILicenseFromImage), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ReadTextFromImage(ReadTextRequest model,
            CancellationToken cancellationToken)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            HttpClient imageClient = _httpClientFactory.CreateClient();

            ILicenseFromImage? licenseFromImage = await httpClient.ReadTextFromImageAsync(
                imageClient,
                model.ImageUrl, model.OcrType, cancellationToken);
            return StatusCode(200, licenseFromImage);
        }
    }
}
