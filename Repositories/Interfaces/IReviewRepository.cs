using MultiVendorAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiVendorAPI.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task CreateReviewAsync(Review review);

        Task<List<Review>> GetReviewsByProductIdAsync(long productId);

        Task<Review?> GetReviewByIdAsync(long reviewId);

        Task DeleteReviewAsync(long reviewId);

        Task<bool> ReviewExistsAsync(long reviewId);

        Task<bool> ProductExistsAsync(long productId);
    }
}
