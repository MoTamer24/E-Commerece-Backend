using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewsController(IReviewService reviewRepository, IUnitOfWork unitOfWork)
        {
            _reviewService = reviewRepository;
            _unitOfWork = unitOfWork;
        }

        // GET: api/reviews/product/5
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsForProduct(int productId)
        {
            var productReviews = await _reviewService.GetReviewsForProductAsync(productId);
            return Ok(productReviews);
        }

        // POST: api/reviews
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto review)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            Guid.TryParse(userId,out Guid UserGuid);
            review.CustomerId=UserGuid;
            await _reviewService.AddReviewAsync(review);
            await _unitOfWork.SaveAllChangesAsync();
            return Ok(review);
        }

        // DELETE: api/reviews/10
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            _reviewService.Delete(id);
            await _unitOfWork.SaveAllChangesAsync();
            return NoContent();
        }
    }
}

