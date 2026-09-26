using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _productService = new ProductService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenProductsExist_ShouldReturnMappedProductCatalogDtos()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Prod 1", Price = 10m, ImageUrl = "img1.png", StockQuantity = 5 },
            new Product { Id = 2, Name = "Prod 2", Price = 20m, ImageUrl = "img2.png", StockQuantity = 10 }
        };

        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);

        // Act
        var result = await _productService.GetAll();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainSingle(p => p.Name == "Prod 1" && p.Price == 10m && p.StockQuantity == 5);
        result.Should().ContainSingle(p => p.Name == "Prod 2" && p.Price == 20m && p.StockQuantity == 10);
    }

    [Fact]
    public async Task GetProductsForCatalogAsync_WhenProductsExist_ShouldReturnCatalogDtos()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Catalog Item", Price = 99m, ImageUrl = "catalog.jpg" }
        };

        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);

        // Act
        var result = await _productService.GetProductsForCatalogAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Catalog Item");
        result.First().Price.Should().Be(99m);
        result.First().ImageUrl.Should().Be("catalog.jpg");
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductExists_ShouldReturnProductDetailsDto()
    {
        // Arrange
        var productId = 1;
        var product = new Product
        {
            Id = productId,
            Name = "Smartphone",
            Description = "Latest model",
            Price = 699m,
            StockQuantity = 15,
            Category = new Category { Id = 2, Name = "Electronics" }
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.id.Should().Be(productId);
        result.Name.Should().Be("Smartphone");
        result.Description.Should().Be("Latest model");
        result.Price.Should().Be(699m);
        result.StockQuantity.Should().Be(15);
        result.categoryName.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var productId = 99;
        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateProductAsync_WhenValidDto_ShouldAddProductAndSaveChanges()
    {
        // Arrange
        var addDto = new AddProductDto
        {
            Name = "New Tablet",
            Description = "10 inch screen",
            Price = 299m,
            StockQuantity = 20,
            CategoryId = 1
        };

        _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _productService.CreateProductAsync(addDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Tablet");
        result.Price.Should().Be(299m);
        result.StockQuantity.Should().Be(20);
        _productRepoMock.Verify(r => r.AddAsync(It.Is<Product>(p => p.Name == "New Tablet" && p.Price == 299m && p.CategoryId == 1)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _productService.CreateProductAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CreateProductAsync_WhenPriceIsZeroOrNegative_ShouldThrowArgumentException(decimal price)
    {
        // Arrange
        var addDto = new AddProductDto
        {
            Name = "Invalid Product",
            Description = "Invalid price",
            Price = price,
            StockQuantity = 5,
            CategoryId = 1
        };

        // Act
        Func<Task> act = async () => await _productService.CreateProductAsync(addDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Price must be a positive number.*");
        _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenProductExists_ShouldUpdatePropertiesAndSaveChanges()
    {
        // Arrange
        var productId = 1;
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Old Product",
            Description = "Old Desc",
            Price = 50m,
            StockQuantity = 10
        };

        var updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Description = "Updated Desc",
            Price = 75m,
            StockQuantity = 12
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);

        // Act
        await _productService.UpdateProductAsync(productId, updateDto);

        // Assert
        existingProduct.Name.Should().Be("Updated Product");
        existingProduct.Description.Should().Be("Updated Desc");
        existingProduct.Price.Should().Be(75m);
        existingProduct.StockQuantity.Should().Be(12);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenProductDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var productId = 99;
        var updateDto = new UpdateProductDto
        {
            Name = "NonExistent",
            Description = "Desc",
            Price = 10m,
            StockQuantity = 1
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _productService.UpdateProductAsync(productId, updateDto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Product with ID {productId} not found.");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductExists_ShouldRemoveProductAndSaveChanges()
    {
        // Arrange
        var productId = 1;
        var product = new Product { Id = productId, Name = "Product to delete" };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        await _productService.DeleteProductAsync(productId);

        // Assert
        _productRepoMock.Verify(r => r.Remove(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var productId = 99;
        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _productService.DeleteProductAsync(productId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Product with ID {productId} not found.");
        _productRepoMock.Verify(r => r.Remove(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }
}
