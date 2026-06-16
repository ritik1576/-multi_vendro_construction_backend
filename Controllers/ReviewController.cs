using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InframartAPI_New.DTOs;
using InframartAPI_New.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace MultiVendorAPI.Controllers
{
    [ApiController]
    [Route("reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("User identification token is invalid or missing.");
            }
            return userId;
        }

        private string GetCurrentUserRole()
        {
            var claim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(claim))
            {
                throw new UnauthorizedAccessException("User role claim is missing.");
            }
            return claim;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var response = await _reviewService.CreateReviewAsync(dto, userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("product/{productId:long}")]
        public async Task<IActionResult> GetProductReviews(long productId)
        {
            var response = await _reviewService.GetReviewsByProductIdAsync(productId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "admin,vendor")]
        public async Task<IActionResult> DeleteReview(long id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var response = await _reviewService.DeleteReviewAsync(id, userId, role);
            return StatusCode(response.StatusCode, response);
        }
    }
}
