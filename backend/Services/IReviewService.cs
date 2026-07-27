using Marketify.Contracts.Review;

namespace Marketify.Services
{
    public interface IReviewService
    {
        Task<bool> AddReviewAsync(string userId, CreateReviewDto reviewDto);
        Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId);
    }
}
