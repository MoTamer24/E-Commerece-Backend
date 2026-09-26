using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class CartServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cartRepoMock = new Mock<ICartRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<CartService>>();

        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _cartService = new CartService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddToCartAsync_WhenCartDoesNotExistAndProductExists_ShouldCreateCartAndAddItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = 1;
        var quantity = 2;

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync((Cart?)null);

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new Product { Id = productId, Name = "Test Product", Price = 10m });

        // Act
        await _cartService.AddToCartAsync(userId, productId, quantity);

        // Assert
        _cartRepoMock.Verify(r => r.AddAsync(It.Is<Cart>(c => c.UserId == userId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_WhenCartExistsAndItemIsNew_ShouldAddCartItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = 1;
        var quantity = 3;

        var existingCart = new Cart
        {
            Id = 10,
            UserId = userId,
            CartItems = new List<CartItem>()
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync(existingCart);

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new Product { Id = productId, Name = "Test Product", Price = 15m });

        // Act
        await _cartService.AddToCartAsync(userId, productId, quantity);

        // Assert
        existingCart.CartItems.Should().HaveCount(1);
        existingCart.CartItems.First().ProductId.Should().Be(productId);
        existingCart.CartItems.First().Quantity.Should().Be(quantity);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_WhenCartExistsAndItemAlreadyPresent_ShouldIncrementQuantity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = 1;
        var initialQuantity = 2;
        var addedQuantity = 3;

        var existingCart = new Cart
        {
            Id = 10,
            UserId = userId,
            CartItems = new List<CartItem>
            {
                new CartItem { ProductId = productId, Quantity = initialQuantity }
            }
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync(existingCart);

        // Act
        await _cartService.AddToCartAsync(userId, productId, addedQuantity);

        // Assert
        existingCart.CartItems.Should().HaveCount(1);
        existingCart.CartItems.First().Quantity.Should().Be(initialQuantity + addedQuantity);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_WhenProductDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = 99;
        var quantity = 1;

        var existingCart = new Cart
        {
            UserId = userId,
            CartItems = new List<CartItem>()
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync(existingCart);

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _cartService.AddToCartAsync(userId, productId, quantity);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Product not found");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartDoesNotExist_ShouldReturnEmptyCartDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync((Cart?)null);

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartExistsWithItems_ShouldReturnMappedCartDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = new Product { Id = 1, Name = "Laptop", Price = 1000m }
                },
                new CartItem
                {
                    ProductId = 2,
                    Quantity = 1,
                    Product = new Product { Id = 2, Name = "Mouse", Price = 50m }
                }
            }
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainSingle(i => i.ProductId == 1 && i.ProductName == "Laptop" && i.Price == 1000m && i.Quantity == 2);
        result.Items.Should().ContainSingle(i => i.ProductId == 2 && i.ProductName == "Mouse" && i.Price == 50m && i.Quantity == 1);
        result.TotalAmount.Should().Be(2050m);
    }

    [Fact]
    public async Task RemoveFromCartAsync_WhenCartDoesNotExist_ShouldDoNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync((Cart?)null);

        // Act
        await _cartService.RemoveFromCartAsync(userId, 1);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveFromCartAsync_WhenItemExistsInCart_ShouldRemoveItemAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = 1;
        var cartItem = new CartItem { ProductId = productId, Quantity = 1 };
        var cart = new Cart
        {
            UserId = userId,
            CartItems = new List<CartItem> { cartItem }
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync(cart);

        // Act
        await _cartService.RemoveFromCartAsync(userId, productId);

        // Assert
        cart.CartItems.Should().NotContain(cartItem);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveFromCartAsync_WhenItemNotInCart_ShouldNotSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            CartItems = new List<CartItem> { new CartItem { ProductId = 1, Quantity = 1 } }
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(userId))
            .ReturnsAsync(cart);

        // Act
        await _cartService.RemoveFromCartAsync(userId, 99);

        // Assert
        cart.CartItems.Should().HaveCount(1);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }
}
