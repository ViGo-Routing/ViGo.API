using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using ViGo.API.SignalR.Core;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.BackgroundTasks;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private PaymentServices paymentServices;
        //private ISignalRService signalRService;
        //private UserServices userServices;

        private IServiceScopeFactory _serviceScopeFactory;
        private ILogger<PaymentController> _logger;
        private IBackgroundTaskQueue _backgroundQueue;

        public PaymentController(IUnitOfWork work, ISignalRService signalRService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PaymentController> logger,
            IBackgroundTaskQueue queue)
        {
            paymentServices = new PaymentServices(work, logger);
            //this.signalRService = signalRService;
            //userServices = new UserServices(work, logger);

            _serviceScopeFactory = serviceScopeFactory;
            //tripMappingServices = new TripMappingServices(work);
            _logger = logger;
            _backgroundQueue = queue;
        }

        /// <summary>
        /// Generate Test Payment URL for VNPay.
        /// </summary>
        /// <remarks>
        /// TESTING ONLY
        /// </remarks>
        /// <returns>
        /// The created URL
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="200">Payment URL is created successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Generate/VnPay")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateVnPayTestPaymentUrl()
        {
            string paymentUrl = paymentServices.GenerateVnPayTestPaymentUrl(HttpContext);
            return StatusCode(200, paymentUrl);
        }

        /// <summary>
        /// VNPay Callback URL
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="200">Payment is processed successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("Callback/VnPay")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> VnPayCallback(CancellationToken cancellationToken)
        {
            string message = await paymentServices.VnPayPaymentCallbackAsync(Request.GetDisplayUrl(), Request.Query, cancellationToken);

            return StatusCode(200, message);
        }

        /// <summary>
        /// VNPay IPN URL
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="200">Payment is processed successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("IPN/VnPay")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
        {
            (string code, string message) = await paymentServices.VnPayPaymentIpnAsync(Request.GetDisplayUrl(),
                Request.Query, _backgroundQueue, _serviceScopeFactory, cancellationToken);

            return StatusCode(200, new { code = code, message = message });
        }
    }
}
