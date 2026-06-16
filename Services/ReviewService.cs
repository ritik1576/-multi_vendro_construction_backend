using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiVendorAPI.Common;
using InframartAPI_New.DTOs;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly INotificationService _notificationService;

        public ReviewService(IReviewRepository reviewRepository, INotificationService notificationService)
        {
            _reviewRepository = reviewRepository;
            _notificationService = notificationService;
        }

        public async Task<ServiceResponse<ReviewResponseDto>> CreateReviewAsync(CreateReviewDto dto, long userId)
        {
            try
            {
                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    return ServiceResponse<ReviewResponseDto>.FailureResponse("Rating must be between 1 and 5", 400);
                }

                if (string.IsNullOrWhiteSpace(dto.Review))
                {
                    return ServiceResponse<ReviewResponseDto>.FailureResponse("Review text cannot be empty", 400);
                }

                var productExists = await _reviewRepository.ProductExistsAsync(dto.ProductId);
                if (!productExists)
                {
                    return ServiceResponse<ReviewResponseDto>.FailureResponse("Product not found", 404);
                }

                var review = new Review
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Rating = dto.Rating,
                    ReviewText = dto.Review,
                    CreatedAt = DateTime.Now
                };

                await _reviewRepository.CreateReviewAsync(review);

                var savedReview = await _reviewRepository.GetReviewByIdAsync(review.Id);
                if (savedReview == null)
                {
                    return ServiceResponse<ReviewResponseDto>.FailureResponse("Failed to save and retrieve the review.", 500);
                }

                // Trigger vendor notification
                try
                {
                    var vendorUserId = savedReview.Product?.Vendor?.UserId;
                    if (vendorUserId.HasValue)
                    {
                        await _notificationService.CreateNotificationAsync(
                            vendorUserId.Value,
                            "New Product Review",
                            "A customer has submitted a review for your product.",
                            "product"
                        );
                    }
                }
                catch (Exception notificationEx)
                {
                    // Log or handle notification error internally so it doesn't break the review creation flow
                    Console.WriteLine($"Notification trigger failed: {notificationEx.Message}");
                }

                var responseDto = new ReviewResponseDto
                {
                    Id = savedReview.Id,
                    UserId = savedReview.UserId,
                    UserName = savedReview.User?.FullName ?? savedReview.User?.Email ?? "Anonymous User",
                    ProductId = savedReview.ProductId,
                    Rating = savedReview.Rating,
                    Review = savedReview.ReviewText,
                    CreatedAt = savedReview.CreatedAt
                };

                return ServiceResponse<ReviewResponseDto>.SuccessResponse(responseDto, "Review submitted successfully", 201);
            }
            catch (Exception ex)
            {
                return ServiceResponse<ReviewResponseDto>.FailureResponse($"Database failure or internal error: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResponse<List<ReviewResponseDto>>> GetReviewsByProductIdAsync(long productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return ServiceResponse<List<ReviewResponseDto>>.FailureResponse("Invalid product id.", 400);
                }

                var reviews = await _reviewRepository.GetReviewsByProductIdAsync(productId);
                var responseDtos = reviews.Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User?.FullName ?? r.User?.Email ?? "Anonymous User",
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Review = r.ReviewText,
                    CreatedAt = r.CreatedAt
                }).ToList();

                return ServiceResponse<List<ReviewResponseDto>>.SuccessResponse(responseDtos, "Reviews retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<ReviewResponseDto>>.FailureResponse($"Database failure or internal error: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteReviewAsync(long reviewId, long currentUserId, string userRole)
        {
            try
            {
                if (reviewId <= 0)
                {
                    return ServiceResponse<bool>.FailureResponse("Invalid review id.", 400);
                }

                var review = await _reviewRepository.GetReviewByIdAsync(reviewId);
                if (review == null)
                {
                    return ServiceResponse<bool>.FailureResponse("Review not found", 404);
                }

                // Role ownership validations
                if (string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Admin can delete any review
                }
                else if (string.Equals(userRole, "vendor", StringComparison.OrdinalIgnoreCase))
                {
                    // Vendor can delete reviews only on products they own
                    var productVendorUserId = review.Product?.Vendor?.UserId;
                    if (!productVendorUserId.HasValue || productVendorUserId.Value != currentUserId)
                    {
                        return ServiceResponse<bool>.FailureResponse("Access denied. You can only delete reviews for products that belong to you.", 403);
                    }
                }
                else
                {
                    return ServiceResponse<bool>.FailureResponse("Access denied. Customers are not authorized to delete reviews.", 403);
                }

                await _reviewRepository.DeleteReviewAsync(reviewId);
                return ServiceResponse<bool>.SuccessResponse(true, "Review deleted successfully", 200);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.FailureResponse($"Database failure or internal error: {ex.Message}", 500);
            }
        }
    }
}
