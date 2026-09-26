using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Identity;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class ReviewServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReviewRepository> _reviewRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly ReviewService _reviewService;

    public ReviewServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _reviewRepoMock = new Mock<IReviewRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.Reviews).Returns(_reviewRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _reviewService = new ReviewService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Delete_WhenReviewExists_ShouldRemoveReviewFromRepository()
    {
        // Arrange
        var reviewId = 1;
        var review = new Review { Id = reviewId, Comment = "Great product", Rating = 5 };

        _reviewRepoMock.Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        // Act
        _reviewService.Delete(reviewId);

        // Allow async void method to process task
        await Task.Delay(50);

        // Assert
        _reviewRepoMock.Verify(r => r.GetByIdAsync(reviewId), Times.Once);
        _reviewRepoMock.Verify(r => r.Remove(review), Times.Once);
    }

    [Fact]
    public async Task Get_WhenReviewExists_ShouldReturnMappedReviewDto()
    {
        // Arrange
        var reviewId = 1;
        var review = new Review
        {
            Id = reviewId,
            Rating = 4,
            Comment = "Good quality",
            User = new ApplicationUser { UserName = "JohnDoe" }
        };

        _reviewRepoMock.Setup(r => r.GetByIdAsync(reviewId))
            .ReturnsAsync(review);

        // Act
        var result = await _reviewService.get(reviewId);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(4);
        result.Comment.Should().Be("Good quality");
        result.ReviewerName.Should().Be("JohnDoe");
    }

    [Fact]
    public async Task GetReviewsForProductAsync_WhenReviewsExist_ShouldReturnMappedReviewDtos()
    {
        // Arrange
        var productId = 10;
        var reviews = new List<Review>
        {
            new Review { Id = 1, ProductId = productId, Rating = 5, Comment = "Awesome!", User = new ApplicationUser { UserName = "Alice" } },
            new Review { Id = 2, ProductId = productId, Rating = 3, Comment = "Average", User = new ApplicationUser { UserName = "Bob" } }
        };

        _reviewRepoMock.Setup(r => r.GetReviewsForProductAsync(productId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _reviewService.GetReviewsForProductAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainSingle(r => r.Id == 1 && r.Rating == 5 && r.Comment == "Awesome!" && r.ReviewerName == "Alice");
        result.Should().ContainSingle(r => r.Id == 2 && r.Rating == 3 && r.Comment == "Average" && r.ReviewerName == "Bob");
    }

    [Fact]
    public async Task AddReviewAsync_WhenProductExists_ShouldAddReviewAndReturnDto()
    {
        // Arrange
        var createReviewDto = new CreateReviewDto
        {
            ProductId = 1,
            CustomerId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Excellent service!"
        };

        var product = new Product { Id = 1, Name = "Smartphone" };

        _productRepoMock.Setup(r => r.GetByIdAsync(createReviewDto.ProductId))
            .ReturnsAsync(product);

        _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _reviewService.AddReviewAsync(createReviewDto);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Excellent service!");
        _reviewRepoMock.Verify(r => r.AddAsync(It.Is<Review>(rev => rev.ProductId == 1 && rev.Rating == 5 && rev.Comment == "Excellent service!")), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_WhenProductDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var createReviewDto = new CreateReviewDto
        {
            ProductId = 99,
            CustomerId = Guid.NewGuid(),
            Rating = 4,
            Comment = "Nice"
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(createReviewDto.ProductId))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _reviewService.AddReviewAsync(createReviewDto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Product with ID 99 not found.");
        _reviewRepoMock.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }
}
