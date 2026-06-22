using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InframartAPI_New.Controllers
{
    [Route("wallet")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Get Current Wallet Balance
        /// </summary>
        /// <remarks>
        /// Returns available balance, locked balance, total credits, total debits, and wallet status for the authenticated user.
        /// </remarks>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized."
                });
            }

            var walletBalance = await _walletService.GetWalletBalanceAsync(userId);
            if (walletBalance == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Wallet not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Wallet balance retrieved successfully.",
                data = walletBalance
            });
        }
    }
}
