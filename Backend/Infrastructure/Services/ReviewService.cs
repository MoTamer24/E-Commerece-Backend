using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async void Delete(int reviewId)
    {
        _logger.LogInformation("Deleting review with ID {ReviewId}", reviewId);
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
        if (review != null)
        {
            _unitOfWork.Reviews.Remove(review);
            _logger.LogInformation("Successfully requested removal of review with ID {ReviewId}", reviewId);
        }
        else
        {
            _logger.LogWarning("Cannot delete review: Review with ID {ReviewId} not found", reviewId);
        }
    }

    public async Task<ReviewDto> get(int reviewId)
    {
        _logger.LogInformation("Fetching review with ID {ReviewId}", reviewId);
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);

        return new ReviewDto
        {
            Comment = review.Comment,
            Rating = review.Rating,
            ReviewerName = review.User?.UserName ?? string.Empty
        };
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsForProductAsync(int productId)
    {
        _logger.LogInformation("Fetching reviews for Product {ProductId}", productId);
        var reviews = await _unitOfWork.Reviews.GetReviewsForProductAsync(productId);
        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewerName = r.User?.UserName ?? string.Empty
        });
    }

    public async Task<ReviewDto> AddReviewAsync(CreateReviewDto reviewDto)
    {
        _logger.LogInformation("Adding review for Product {ProductId} by Customer {CustomerId}", reviewDto.ProductId, reviewDto.CustomerId);

        var product = await _unitOfWork.Products.GetByIdAsync(reviewDto.ProductId);
        if (product == null)
        {
            _logger.LogWarning("Failed to add review: Product {ProductId} not found", reviewDto.ProductId);
            throw new KeyNotFoundException($"Product with ID {reviewDto.ProductId} not found.");
        }

        var reviewEntity = new Review
        {
            Rating = reviewDto.Rating,
            Comment = reviewDto.Comment,
            ProductId = reviewDto.ProductId
        };

        await _unitOfWork.Reviews.AddAsync(reviewEntity);
        _logger.LogInformation("Successfully added review for Product {ProductId}", reviewDto.ProductId);

        return new ReviewDto { Id = reviewEntity.Id, Rating = reviewEntity.Rating, Comment = reviewEntity.Comment };
    }
}