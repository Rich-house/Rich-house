using Marketify.Contracts.Review;
using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services
{
    public class ReviewService(ApplicationDbContext context) : IReviewService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<bool> AddReviewAsync(string userId, CreateReviewDto reviewDto)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == reviewDto.ProductId);
            var alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ProductId == reviewDto.ProductId && r.UserId == userId);
            if (alreadyReviewed) return false;
            var review = new Review
            {
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                ProductId = reviewDto.ProductId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Reviews.Add(review);
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async  Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            return await _context.Reviews
         .Where(r => r.ProductId == productId)
         .OrderByDescending(r => r.CreatedAt) 
         .Select(r => new ReviewDto
         {
             Id = r.Id,
             Rating = r.Rating,
             Comment = r.Comment,
             CreatedAt = r.CreatedAt,
             UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anonymous"
         })
         .ToListAsync();
        }
    }
}
