using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Infrastructure.Services;

public class ReviewService : IReviewService
{


   private readonly IUnitOfWork _unitOfWork;

        public ReviewService( IUnitOfWork unitOfWork)
    {
        
         _unitOfWork = unitOfWork;
    }

    public async void Delete(int reviewId)
    {
        var review = await  _unitOfWork.Reviews.GetByIdAsync(reviewId);
      _unitOfWork.Reviews.Remove(review);
    }


      public async Task<ReviewDto> get(int reviewId)
    {
        var review = await  _unitOfWork.Reviews.GetByIdAsync(reviewId);

        return new ReviewDto()
        {
            Comment=review.Comment,
            Rating=review.Rating,
            ReviewerName=review.User.UserName,
        };
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsForProductAsync(int productId)
    {
        var reviews = await  _unitOfWork.Reviews.GetReviewsForProductAsync(productId);
        return reviews.Select(r => new ReviewDto 
        { 
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewerName = r.User.UserName
        });
    }

    public async Task<ReviewDto> AddReviewAsync(CreateReviewDto reviewDto)
    {
        // Business Logic: Ensure the product exists before adding a review
        var product = await  _unitOfWork.Products.GetByIdAsync(reviewDto.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {reviewDto.ProductId} not found.");
        }

        var reviewEntity = new Review
        {
            Rating = reviewDto.Rating,
            Comment = reviewDto.Comment,
            ProductId = reviewDto.ProductId
            // CustomerId would be set here
        };

        await  _unitOfWork.Reviews.AddAsync(reviewEntity);
       

        return new ReviewDto { Id = reviewEntity.Id, Rating = reviewEntity.Rating, Comment = reviewEntity.Comment };
    }
}