using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private PaymentServices paymentServices;

        public PaymentController(IUnitOfWork work)
        {
            paymentServices = new PaymentServices(work);
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
            try
            {
                string paymentUrl = paymentServices.GenerateVnPayTestPaymentUrl(HttpContext);
                return StatusCode(200, paymentUrl);
            }
            catch (ApplicationException appEx)
            {
                return StatusCode(400, appEx.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
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
        public async Task<IActionResult> VnPayCallback()
        {
            try
            {
                await paymentServices.VnPayPaymentConfirmAsync(Request.Query);
                return StatusCode(204);
            }
            catch (ApplicationException appEx)
            {
                return StatusCode(400, appEx.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }
    }
}
