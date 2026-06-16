using System.Collections.Generic;
using System.Threading.Tasks;
using MultiVendorAPI.Common;
using InframartAPI_New.DTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ServiceResponse<ReviewResponseDto>> CreateReviewAsync(CreateReviewDto dto, long userId);

        Task<ServiceResponse<List<ReviewResponseDto>>> GetReviewsByProductIdAsync(long productId);

        Task<ServiceResponse<bool>> DeleteReviewAsync(long reviewId, long currentUserId, string userRole);
    }
}
