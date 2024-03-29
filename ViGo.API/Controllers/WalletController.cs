﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViGo.Domain;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Wallets;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private WalletServices walletServices;
        private ILogger<WalletController> _logger;
        public WalletController(IUnitOfWork work, ILogger<WalletController> logger)
        {
            walletServices = new WalletServices(work, logger);
            _logger = logger;
        }

        //[ProducesResponseType(typeof(WalletViewModel), 200)]
        //[HttpPost]
        //public async Task<IActionResult> CreateWalletAsync(Guid userId)
        //{
        //    Wallet wallet = await walletServices.CreateWalletAsync(userId);
        //    return StatusCode(200, wallet);
        //}


        /// <summary>
        /// Get list of Wallet
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get list of Wallet successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(IPagedEnumerable<WalletViewModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllWalletsAsync(
            [FromQuery] PaginationParameter pagination, CancellationToken cancellationToken)
        {
            //if (pagination is null)
            //{
            //    pagination = PaginationParameter.Default;
            //}

            IPagedEnumerable<WalletViewModel> listWallet = await walletServices.GetAllWalletsAsync(pagination, HttpContext, cancellationToken);
            return StatusCode(200, listWallet);
        }

        /// <summary>
        /// Get Wallet of User by UserID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Get Wallet successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(WalletViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "CUSTOMER,DRIVER,ADMIN")]
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetWalletByUserId(Guid userId,
            CancellationToken cancellationToken)
        {
            WalletViewModel wallet = await walletServices.GetWalletByUserId(userId, cancellationToken);
            return StatusCode(200, wallet);
        }

        /// <summary>
        /// Update Wallet of User by UserID
        /// </summary>
        /// <response code="401">Login failed</response>
        /// <response code="400">Some information is invalid</response>
        /// <response code="200">Updated successfully</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(WalletViewModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWalletStatusById(Guid id,
            WalletUpdateModel walletStatusUpdate, CancellationToken cancellationToken)
        {
            WalletViewModel wallet = await walletServices
                .UpdateWalletStatusById(id, walletStatusUpdate, cancellationToken);
            return StatusCode(200, wallet);
        }

        /// <summary>
        /// Get System Wallet analysis data.
        /// </summary>
        /// <remarks>Only admin 
        /// </remarks>
        /// <returns>
        /// System Wallet analysis information
        /// </returns>
        /// <response code="400">Some information has gone wrong</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="200">Get System wallet analysis successfully</response>
        /// <response code="500">Server error</response>
        [HttpGet("SystemAnalysis")]
        [ProducesResponseType(typeof(SystemWalletAnalysisModel), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetSystemWalletAnalysis(/*SystemWalletAnalysisRequest request,*/
            CancellationToken cancellationToken)
        {
            SystemWalletAnalysisModel analysisModel = await walletServices
                .GetSystemWalletAnalysisAsync(cancellationToken);
            //IEnumerable<SystemWalletAnalysises> analysises = await
            //    walletServices.GetSystemWalletAnalysisesAsync(request, cancellationToken);

            return StatusCode(200, analysisModel);
        }
    }
}
