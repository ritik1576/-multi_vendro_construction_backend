using InframartAPI_New.Services.Interfaces;
using InframartAPI_New.DTOs;
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

        /// <summary>
        /// Get Wallet Transactions
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of wallet transactions for the authenticated user.
        /// </remarks>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized."
                });
            }

            var result = await _walletService.GetWalletTransactionsAsync(userId, page, pageSize);
            if (result == null)
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
                message = "Wallet transactions retrieved successfully.",
                data = result.Value.items,
                totalCount = result.Value.totalCount,
                page,
                pageSize
            });
        }

        /// <summary>
        /// Add Money to Wallet
        /// </summary>
        /// <remarks>
        /// Deposits money into the authenticated user's wallet and logs a credit transaction.
        /// </remarks>
        [HttpPost("add-money")]
        public async Task<IActionResult> AddMoney([FromBody] AddMoneyRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized."
                });
            }

            var (success, message, wallet) = await _walletService.AddMoneyAsync(userId, dto);
            if (!success)
            {
                if (message == "Wallet not found.")
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message,
                data = wallet
            });
        }

        /// <summary>
        /// Withdraw Money from Wallet
        /// </summary>
        /// <remarks>
        /// Withdraws money from the authenticated user's wallet and logs a debit transaction.
        /// </remarks>
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawMoneyRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized."
                });
            }

            var (success, message, wallet) = await _walletService.WithdrawMoneyAsync(userId, dto);
            if (!success)
            {
                if (message == "Wallet not found.")
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message,
                data = wallet
            });
        }

        /// <summary>
        /// Transfer Money to Vendor
        /// </summary>
        /// <remarks>
        /// Transfers money from the authenticated user's wallet to a target vendor's wallet.
        /// </remarks>
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferMoneyRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized."
                });
            }

            var (success, message, wallet) = await _walletService.TransferMoneyAsync(userId, dto);
            if (!success)
            {
                if (message == "Sender wallet not found." || message == "Recipient wallet not found.")
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message,
                data = wallet
            });
        }
    }
}
