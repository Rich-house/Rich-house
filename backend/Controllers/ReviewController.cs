using Marketify.Contracts.Review;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController(IReviewService reviewService) : ControllerBase
    {
        private readonly IReviewService _reviewService = reviewService;
    
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Create(CreateReviewDto reviewDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reviewService.AddReviewAsync(userId, reviewDto);

            if (!result) return BadRequest("The Review on This Product Already Exsisted!");

            return Ok(new { message = "Review added successfully" });
        }
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var reviews = await _reviewService.GetProductReviewsAsync(productId);

            return Ok(reviews);
        }
    } } 
