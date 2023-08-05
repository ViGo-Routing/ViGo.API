using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Data;
using ViGo.API.SignalR.Core;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.WalletTransactions;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.BackgroundTasks;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;
using ViGo.Utilities.Payments;

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

        private ISchedulerFactory schedulerFactory;


        public PaymentController(IUnitOfWork work, ISignalRService signalRService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PaymentController> logger,
            IBackgroundTaskQueue queue, ISchedulerFactory schedulerFactory)
        {
            paymentServices = new PaymentServices(work, logger);
            //this.signalRService = signalRService;
            //userServices = new UserServices(work, logger);

            _serviceScopeFactory = serviceScopeFactory;
            //tripMappingServices = new TripMappingServices(work);
            _logger = logger;
            _backgroundQueue = queue;
            this.schedulerFactory = schedulerFactory;
        }

        /// <summary>
        /// Create Topup Transaction and get the payment URL
        /// </summary>
        /// <remarks>
        /// Allowed Payment Method: ZALO or VNPAY
        /// <br />
        /// If the current logged-in user is Admin, UserId is required
        /// </remarks>
        /// <returns>
        /// The created topup transaction
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Transaction URL is created successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Generate/Topup")]
        [ProducesResponseType(typeof(TopupTransactionViewModel), 200)]
        [Authorize]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateTopupTransaction(TopupTransactionCreateModel model,
            CancellationToken cancellationToken)
        {
            (TopupTransactionViewModel? viewModel, string clientIpAddress) = await paymentServices
                .CreateTopUpTransactionRequest(model, HttpContext, cancellationToken);

            if (viewModel != null && !string.IsNullOrEmpty(clientIpAddress))
            {
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
   
                        // Schedule transaction status checking
                        IScheduler scheduler = await schedulerFactory.GetScheduler(token);
                        await backgroundServices.ScheduleCheckTransactionStatusAsync(viewModel.Id, clientIpAddress, scheduler, token);
                    }
                });
            }
            return StatusCode(200, viewModel);
        }

        #region VnPay

        ///// <summary>
        ///// Generate Payment URL for VNPay.
        ///// </summary>
        ///// <remarks>
        ///// </remarks>
        ///// <returns>
        ///// The created URL
        ///// </returns>
        ///// <response code="400">Some information is not valid</response>
        ///// <response code="200">Payment URL is created successfully</response>
        ///// <response code="500">Server error</response>
        //[HttpPost("Generate/VnPay")]
        //[ProducesResponseType(typeof(string), 200)]
        //[ProducesResponseType(400)]
        //[ProducesResponseType(500)]
        //public async Task<IActionResult> GenerateVnPayPaymentUrl(TopupTransactionCreateModel model,
        //    CancellationToken cancellationToken)
        //{
        //    string paymentUrl = await paymentServices.GenerateVnPayPaymentUrlAsync(model, PaymentMethod.VNPAY, HttpContext, 
        //        cancellationToken);
        //    return StatusCode(200, paymentUrl);
        //}

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
            //(string code, string message, Guid? userId) = await paymentServices.VnPayPaymentIpnAsync(Request.GetDisplayUrl(),
            //    Request.Query, /*_backgroundQueue, _serviceScopeFactory,*/ cancellationToken);

            //if (userId != null)
            //{
            //    // Check for pending withdrawal transactions
            //    await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
            //    {
            //        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
            //        {
            //            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
            //            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
            //            await backgroundServices.CheckForPendingTransactions(userId.Value, token);
            //        }
            //    });
            //}

            //return StatusCode(200, new { RspCode = code, Message = message });
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
        // Not tested yet!
        public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
        {
            (string code, string message, Guid? userId) = await paymentServices.VnPayPaymentIpnAsync(Request.GetDisplayUrl(),
                Request.Query, /*_backgroundQueue, _serviceScopeFactory,*/ cancellationToken);

            if (userId != null)
            {
                // Check for pending withdrawal transactions
                await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                        BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                        await backgroundServices.CheckForPendingTransactionsAsync(userId.Value, token);
                    }
                });
            }

            return StatusCode(200, new { RspCode = code, Message = message });
        }
        #endregion

        #region ZaloPay
        ///// <summary>
        ///// Create ZaloPay Order
        ///// </summary>
        ///// <remarks>
        ///// </remarks>
        ///// <returns>
        ///// The created order with Order URL
        ///// </returns>
        ///// <response code="400">Some information is not valid</response>
        ///// <response code="401">Unauthorized</response>
        ///// <response code="200">Order URL is created successfully</response>
        ///// <response code="500">Server error</response>
        //[HttpPost("Generate/ZaloPay")]
        //[ProducesResponseType(typeof(TopupTransactionViewModel), 200)]
        //[Authorize]
        //[ProducesResponseType(400)]
        //[ProducesResponseType(401)]
        //[ProducesResponseType(500)]
        //public async Task<IActionResult> GenerateZaloPayOrder(TopupTransactionCreateModel model,
        //    CancellationToken cancellationToken)
        //{
        //    TopupTransactionViewModel? viewModel = await paymentServices
        //        .CreateTopUpTransactionRequest(model, PaymentMethod.ZALO, HttpContext, cancellationToken);

        //    return StatusCode(200, viewModel);
        //}

        /// <summary>
        /// ZaloPay Callback URL
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="200">Payment is processed successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Callback/ZaloPay")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(ZaloPayCallbackResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ZaloPayCallback(
            ZaloPayCallbackModel model,
            CancellationToken cancellationToken)
        {
            ZaloPayCallbackResponse response = await paymentServices.ZaloPayCallback(model, cancellationToken);
            return StatusCode(200, response);
        }

        /// <summary>
        /// ZaloPay Query Order status
        /// </summary>
        /// <returns>
        /// Param as the wallet transaction Id
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="200">Payment is queried successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Query/ZaloPay/{walletTransactionId}")]
        [ProducesResponseType(typeof(ZaloPayQueryViGoResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ZaloPayQuery(
            Guid walletTransactionId,
            CancellationToken cancellationToken)
        {
            ZaloPayQueryViGoResponse response = await paymentServices.ZaloPayGetOrderStatus(walletTransactionId, 
                HttpContext,
                cancellationToken);
            return StatusCode(200, response);
        }

        /// <summary>
        /// VnPay Query Order status
        /// </summary>
        /// <returns>
        /// Param as the wallet transaction Id
        /// </returns>
        /// <response code="400">Some information is not valid</response>
        /// <response code="200">Payment is queried successfully</response>
        /// <response code="500">Server error</response>
        [HttpPost("Query/VnPay/{walletTransactionId}")]
        [ProducesResponseType(typeof(VnPayQueryViGoResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> VnPayQuery(
            Guid walletTransactionId,
            CancellationToken cancellationToken)
        {
            VnPayQueryViGoResponse response = await paymentServices
                .GetVnPayTransactionStatus(walletTransactionId, HttpContext, cancellationToken);
            return StatusCode(200, response);
        }
        #endregion
    }
}
