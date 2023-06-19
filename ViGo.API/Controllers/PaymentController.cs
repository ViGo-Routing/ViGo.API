using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using ViGo.API.BackgroundTasks;
using ViGo.API.SignalR.Core;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private PaymentServices paymentServices;
        private ISignalRService signalRService;
        private UserServices userServices;

        private IServiceScopeFactory _serviceScopeFactory;
        private ILogger<BookingController> _logger;
        private IBackgroundTaskQueue _backgroundQueue;

        public PaymentController(IUnitOfWork work, ISignalRService signalRService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BookingController> logger,
            IBackgroundTaskQueue queue)
        {
            paymentServices = new PaymentServices(work);
            this.signalRService = signalRService;
            userServices = new UserServices(work);

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
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> VnPayCallback(CancellationToken cancellationToken)
        {
            Booking? booking = await paymentServices.VnPayPaymentConfirmAsync(Request.Query, cancellationToken);
            if (booking != null)
            {
                try
                {
                    // TODO Code

                    // Send notification to user
                    string? fcmToken = await userServices.GetUserFcmToken(booking.CustomerId, cancellationToken);
                    if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                    {
                        await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thành công",
                            "Quý khách đã thực hiện thanh toán đơn đặt chuyến đi bằng VNPay thành công!!", cancellationToken: cancellationToken);

                        // Send data to mobile application
                        await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, new Dictionary<string, string>()
                        {
                            { "bookingId", booking.Id.ToString() },
                            { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                            { "isSuccess", "true" },
                            { "message", "Thanh toán bằng VNPay thành công!" }
                        }, cancellationToken);

                        await _backgroundQueue.QueueBackGroundWorkItemAsync(async token =>
                        {
                            await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                            {
                                IUnitOfWork work = new UnitOfWork(scope.ServiceProvider);
                                TripMappingServices tripMappingServices = new TripMappingServices(work);
                                await tripMappingServices.MapBooking(booking, _logger);
                            }
                        });
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Payment VNPAY Callback Error - {ex.GeneratorErrorMessage()}");
                }
            }

            return StatusCode(204);
        }
    }
}
