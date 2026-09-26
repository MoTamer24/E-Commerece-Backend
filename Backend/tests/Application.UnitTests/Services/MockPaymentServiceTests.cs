using System.Linq.Expressions;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class MockPaymentServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<ILogger<MockPaymentService>> _loggerMock;
    private readonly MockPaymentService _paymentService;

    public MockPaymentServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _loggerMock = new Mock<ILogger<MockPaymentService>>();

        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Payments).Returns(_paymentRepoMock.Object);

        _paymentService = new MockPaymentService(_configMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_WhenOrderExists_ShouldSaveChangesAndReturnGuidString()
    {
        // Arrange
        var orderId = 1;
        var order = new Order { Id = orderId, TotalAmount = 100m };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _paymentService.CreateOrUpdatePaymentIntent(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        Guid.TryParse(result, out _).Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_WhenOrderDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var orderId = 99;
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _paymentService.CreateOrUpdatePaymentIntent(orderId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Order not found");
        _unitOfWorkMock.Verify(u => u.SaveAllChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPaymentDetailsForOrderAsync_WhenPaymentExists_ShouldReturnMappedPaymentDto()
    {
        // Arrange
        var orderId = 1;
        var paymentDate = DateTime.UtcNow;
        var payment = new Payment
        {
            Id = 10,
            OrderId = orderId,
            Amount = 150m,
            PaymentDate = paymentDate,
            Status = "Completed"
        };

        _paymentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetPaymentDetailsForOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.Amount.Should().Be(150m);
        result.PaymentDate.Should().Be(paymentDate);
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetPaymentDetailsForOrderAsync_WhenPaymentDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var orderId = 99;
        _paymentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _paymentService.GetPaymentDetailsForOrderAsync(orderId);

        // Assert
        result.Should().BeNull();
    }
}
