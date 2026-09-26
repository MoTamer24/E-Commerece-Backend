using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cartRepoMock = new Mock<ICartRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _orderService = new OrderService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenCartHasValidItemsAndStock_ShouldCreateOrderAndDeductStock()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId1 = 1;
        var productId2 = 2;

        var cart = new Cart
        {
            UserId = customerId,
            CartItems = new List<CartItem>
            {
                new CartItem { ProductId = productId1, Quantity = 2 },
                new CartItem { ProductId = productId2, Quantity = 1 }
            }
        };

        var product1 = new Product { Id = productId1, Name = "Laptop", Price = 1000m, StockQuantity = 10 };
        var product2 = new Product { Id = productId2, Name = "Mouse", Price = 50m, StockQuantity = 5 };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(customerId))
            .ReturnsAsync(cart);

        _productRepoMock.Setup(r => r.GetProductsById(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        // Act
        var result = await _orderService.CreateOrderAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Pending Payment");
        result.TotalAmount.Should().Be(2050m);

        product1.StockQuantity.Should().Be(8);
        product2.StockQuantity.Should().Be(4);

        _orderRepoMock.Verify(r => r.AddAsync(It.Is<Order>(o => o.UserId == customerId && o.TotalAmount == 2050m)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenProductNotInDb_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = customerId,
            CartItems = new List<CartItem> { new CartItem { ProductId = 99, Quantity = 1 } }
        };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(customerId))
            .ReturnsAsync(cart);

        _productRepoMock.Setup(r => r.GetProductsById(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product>());

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderAsync(customerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Product with ID 99 not found*");
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenStockIsInsufficient_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = 1;
        var cart = new Cart
        {
            UserId = customerId,
            CartItems = new List<CartItem> { new CartItem { ProductId = productId, Quantity = 5 } }
        };

        var product = new Product { Id = productId, Name = "Monitor", Price = 200m, StockQuantity = 2 };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(customerId))
            .ReturnsAsync(cart);

        _productRepoMock.Setup(r => r.GetProductsById(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderAsync(customerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Not enough stock*");
        product.StockQuantity.Should().Be(2); // Stock remains unchanged
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenDbSaveFails_ShouldThrowWrappedException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = customerId,
            CartItems = new List<CartItem> { new CartItem { ProductId = 1, Quantity = 1 } }
        };

        var product = new Product { Id = 1, Name = "Headphones", Price = 100m, StockQuantity = 5 };

        _cartRepoMock.Setup(r => r.GetCartByCustomerId(customerId))
            .ReturnsAsync(cart);

        _productRepoMock.Setup(r => r.GetProductsById(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock.Setup(u => u.SaveAllChangesAsync())
            .ThrowsAsync(new Exception("Database connection failure"));

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderAsync(customerId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("DB updates happened");
    }

    [Fact]
    public async Task GetOrdersForCustomerAsync_WhenOrdersExist_ShouldReturnMappedSummaries()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new Order { Id = 1, UserId = customerId, TotalAmount = 100m, Status = "Completed", OrderDate = DateTime.UtcNow },
            new Order { Id = 2, UserId = customerId, TotalAmount = 200m, Status = "Pending", OrderDate = DateTime.UtcNow }
        };

        _orderRepoMock.Setup(r => r.GetOrdersByCustomerId(customerId))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetOrdersForCustomerAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainSingle(o => o.TotalAmount == 100m && o.Status == "Completed");
        result.Should().ContainSingle(o => o.TotalAmount == 200m && o.Status == "Pending");
    }

    [Fact]
    public async Task GetOrderDetailsAsync_WhenOrderExists_ShouldReturnMappedOrderDetailsDto()
    {
        // Arrange
        var orderId = 1;
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            TotalAmount = 150m,
            Status = "Processing",
            ShippingCity = "New York",
            ShippingCountry = "USA",
            ShippingStreet = "123 Main St",
            ShippingPostalCode = "10001",
            OrderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2, PriceAtPurchase = 75m } }
        };

        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetOrderDetailsAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.TotalAmount.Should().Be(150m);
        result.Status.Should().Be("Processing");
        result.ShippingCity.Should().Be("New York");
        result.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrderDetailsAsync_WhenOrderDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var orderId = 99;
        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _orderService.GetOrderDetailsAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("no Order with this Id");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenOrderExists_ShouldUpdateStatusAndSaveChanges()
    {
        // Arrange
        var orderId = 1;
        var order = new Order { Id = orderId, Status = "Pending Payment" };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        await _orderService.UpdateOrderStatusAsync(orderId, "Processing");

        // Assert
        order.Status.Should().Be("Processing");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenOrderDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var orderId = 99;
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _orderService.UpdateOrderStatusAsync(orderId, "Paid");

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("no Order with this Id");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenSaveAllChangesFails_ShouldThrowWrappedException()
    {
        // Arrange
        var orderId = 1;
        var order = new Order { Id = orderId, Status = "Pending" };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _unitOfWorkMock.Setup(u => u.SaveAllChangesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _orderService.UpdateOrderStatusAsync(orderId, "Paid");

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Error updating order status *");
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderIsPending_ShouldRestoreStockAndUpdateStatus()
    {
        // Arrange
        var orderId = 1;
        var productId = 10;
        var order = new Order
        {
            Id = orderId,
            Status = "Pending Payment",
            OrderItems = new List<OrderItem>
            {
                new OrderItem { ProductId = productId, Quantity = 3 }
            }
        };

        var product = new Product { Id = productId, Name = "Keyboard", StockQuantity = 5 };

        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync(order);

        _productRepoMock.Setup(r => r.GetProductsById(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _orderService.CancelOrderAsync(orderId);

        // Assert
        order.Status.Should().Be("Cancelled");
        product.StockQuantity.Should().Be(8);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var orderId = 99;
        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _orderService.CancelOrderAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Order with ID 99 not found.*");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderIsShipped_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = 1;
        var order = new Order { Id = orderId, Status = "Shipped" };

        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _orderService.CancelOrderAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot cancel an order with status 'Shipped'.*");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderIsAlreadyCancelled_ShouldReturnWithoutModifyingStock()
    {
        // Arrange
        var orderId = 1;
        var order = new Order { Id = orderId, Status = "Cancelled" };

        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync(order);

        // Act
        await _orderService.CancelOrderAsync(orderId);

        // Assert
        _productRepoMock.Verify(r => r.GetProductsById(It.IsAny<List<int>>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenDbSaveFails_ShouldThrowWrappedException()
    {
        // Arrange
        var orderId = 1;
        var order = new Order
        {
            Id = orderId,
            Status = "Pending",
            OrderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1 } }
        };

        var product = new Product { Id = 1, StockQuantity = 2 };

        _orderRepoMock.Setup(r => r.GetOrderDetails(orderId))
            .ReturnsAsync(order);

        _productRepoMock.Setup(r => r.GetProductsById(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock.Setup(u => u.SaveAllChangesAsync())
            .ThrowsAsync(new Exception("Concurrency Exception"));

        // Act
        Func<Task> act = async () => await _orderService.CancelOrderAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*modified by another user*");
    }
}
