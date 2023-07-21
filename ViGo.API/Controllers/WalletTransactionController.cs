using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ViGo.Domain;
using ViGo.Models.Wallets;
using ViGo.Models.WalletTransactions;
using ViGo.Repository.Core;
using ViGo.Models.QueryString.Pagination;
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
        [HttpGet("Wallet/{walletId}")]
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

        /// <summary>
        /// Get details of a Wallet Transaction
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get details of Wallet Transaction successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(WalletTransactionViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize]
        [HttpGet("{walletTransactionId}")]
        public async Task<IActionResult> GetWalletTransaction(Guid walletTransactionId,
            CancellationToken cancellationToken)
        {
            WalletTransactionViewModel walletTransaction = await walletTransactionServices
                .GetTransactionAsync(walletTransactionId, cancellationToken);
            return StatusCode(200, walletTransaction);
        }
    }
}
