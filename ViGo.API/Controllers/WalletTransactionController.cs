using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ViGo.Domain;
using ViGo.Models.Wallets;
using ViGo.Models.WalletTransactions;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletTransactionController : ControllerBase
    {
        private WalletTransactionServices walletTransactionServices;
        private ILogger<WalletTransactionController> _logger;

        public WalletTransactionController(IUnitOfWork work, ILogger<WalletTransactionController> logger)
        {
            walletTransactionServices = new WalletTransactionServices(work, logger);
            _logger = logger;
        }

        /// <summary>
        /// Get list of Wallet Transactions
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list of Wallet Transactions successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<WalletTransactionViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet("{walletId}")]
        public async Task<IActionResult> GetAllWalletTransactionsAsync(Guid walletId,
            [FromQuery] PaginationParameter? pagination, CancellationToken cancellationToken)
        {
            if (pagination is null)
            {
                pagination = PaginationParameter.Default;
            }
            IPagedEnumerable<WalletTransactionViewModel> walletTransactions = await 
                walletTransactionServices.GetAllWalletTransactionsAsync(walletId,
                    pagination, HttpContext, cancellationToken);
            return StatusCode(200, walletTransactions);
        }
    }
}
