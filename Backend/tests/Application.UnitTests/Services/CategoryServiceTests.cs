using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _categoryRepoMock = new Mock<ICategoryRepository>();

        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _categoryService = new CategoryService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WhenCategoriesExist_ShouldReturnCategoryDtoList()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Clothing" }
        };

        _categoryRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainSingle(c => c.Id == 1 && c.Name == "Electronics");
        result.Should().ContainSingle(c => c.Id == 2 && c.Name == "Clothing");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCategoryExists_ShouldReturnCategoryDto()
    {
        // Arrange
        var categoryId = 1;
        var category = new Category { Id = categoryId, Name = "Books" };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.GetCategoryByIdAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(categoryId);
        result.Name.Should().Be("Books");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCategoryDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var categoryId = 99;
        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _categoryService.GetCategoryByIdAsync(categoryId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenValidDto_ShouldAddCategoryAndSaveChanges()
    {
        // Arrange
        var createDto = new CreateCategoryDto { Name = "Toys" };

        _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _categoryService.CreateCategoryAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Toys");
        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Toys")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryExists_ShouldUpdateNameAndSaveChanges()
    {
        // Arrange
        var categoryId = 1;
        var category = new Category { Id = categoryId, Name = "Old Name" };
        var updateDto = new UpdateCategoryDto { Name = "New Name" };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        await _categoryService.UpdateCategoryAsync(categoryId, updateDto);

        // Assert
        category.Name.Should().Be("New Name");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var categoryId = 99;
        var updateDto = new UpdateCategoryDto { Name = "NonExistent" };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _categoryService.UpdateCategoryAsync(categoryId, updateDto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Category not found.");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryExists_ShouldRemoveCategoryAndSaveChanges()
    {
        // Arrange
        var categoryId = 1;
        var category = new Category { Id = categoryId, Name = "Sports" };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        await _categoryService.DeleteCategoryAsync(categoryId);

        // Assert
        _categoryRepoMock.Verify(r => r.Remove(category), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var categoryId = 99;
        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _categoryService.DeleteCategoryAsync(categoryId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Category not found.");
        _categoryRepoMock.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }
}
