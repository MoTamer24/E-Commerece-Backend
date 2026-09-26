using Application.Interfaces.Services;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderSummaryDto> CreateOrderAsync(Guid customerId)
    {
        _logger.LogInformation("Creating order for Customer {CustomerId}", customerId);

        var cart = await _unitOfWork.Carts.GetCartByCustomerId(customerId);
        var customerCartItems = cart.CartItems;

        var productIds = customerCartItems.Select(item => item.ProductId).ToList();
        var productsFromDb = await _unitOfWork.Products.GetProductsById(productIds);
        var productDict = productsFromDb.ToDictionary(p => p.Id);

        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var cartItem in customerCartItems)
        {
            if (!productDict.TryGetValue(cartItem.ProductId, out var product))
            {
                _logger.LogError("Failed to create order: Product with ID {ProductId} not found", cartItem.ProductId);
                throw new InvalidOperationException($"Product with ID {cartItem.ProductId} not found.");
            }

            if (product.StockQuantity < cartItem.Quantity)
            {
                _logger.LogWarning("Failed to create order: Insufficient stock for product {ProductName}. Available: {AvailableStock}, Requested: {RequestedQuantity}", product.Name, product.StockQuantity, cartItem.Quantity);
                throw new InvalidOperationException($"Not enough stock for product: {product.Name}. Available: {product.StockQuantity}, Requested: {cartItem.Quantity}");
            }

            totalAmount += product.Price * cartItem.Quantity;
            product.StockQuantity -= cartItem.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = cartItem.Quantity,
                PriceAtPurchase = product.Price
            });
        }

        var newOrder = new Order
        {
            UserId = customerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            Status = "Pending Payment",
            OrderItems = orderItems
        };

        await _unitOfWork.Orders.AddAsync(newOrder);

        try
        {
            await _unitOfWork.SaveAllChangesAsync();
            _logger.LogInformation("Successfully created order {OrderId} for Customer {CustomerId} with Total {TotalAmount}", newOrder.Id, customerId, totalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save order for Customer {CustomerId}", customerId);
            throw new Exception("DB updates happened", ex);
        }

        return new OrderSummaryDto
        {
            OrderId = newOrder.Id,
            OrderDate = newOrder.OrderDate,
            Status = newOrder.Status,
            TotalAmount = newOrder.TotalAmount
        };
    }

    public async Task<IEnumerable<OrderSummaryDto>> GetOrdersForCustomerAsync(Guid customerId)
    {
        _logger.LogInformation("Fetching orders for Customer {CustomerId}", customerId);
        var orders = await _unitOfWork.Orders.GetOrdersByCustomerId(customerId);
        return orders.Select(order => new OrderSummaryDto
        {
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount
        });
    }

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(int orderId)
    {
        _logger.LogInformation("Fetching details for Order {OrderId}", orderId);
        var order = await _unitOfWork.Orders.GetOrderDetails(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order details not found for Order {OrderId}", orderId);
            throw new Exception("no Order with this Id");
        }

        var orderDetails = new OrderDetailsDto
        {
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            OrderItems = order.OrderItems,
            Id = order.Id,
            Payment = order.Payment,
            ShippingCity = order.ShippingCity,
            ShippingCountry = order.ShippingCountry,
            ShippingPostalCode = order.ShippingPostalCode,
            ShippingStreet = order.ShippingStreet
        };
        return orderDetails;
    }

    public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        _logger.LogInformation("Updating status for Order {OrderId} to {NewStatus}", orderId, newStatus);
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Failed to update status: Order {OrderId} not found", orderId);
            throw new Exception("no Order with this Id");
        }

        order.Status = newStatus;
        try
        {
            await _unitOfWork.SaveAllChangesAsync();
            _logger.LogInformation("Successfully updated status for Order {OrderId} to {NewStatus}", orderId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for Order {OrderId}", orderId);
            throw new Exception("Error updating order status ", ex);
        }
    }

    public async Task CancelOrderAsync(int orderId)
    {
        _logger.LogInformation("Cancelling Order {OrderId}", orderId);
        var order = await _unitOfWork.Orders.GetOrderDetails(orderId);

        if (order == null)
        {
            _logger.LogWarning("Failed to cancel order: Order {OrderId} not found", orderId);
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (order.Status == "Shipped" || order.Status == "Completed")
        {
            _logger.LogWarning("Cannot cancel Order {OrderId} with status '{OrderStatus}'", orderId, order.Status);
            throw new InvalidOperationException($"Cannot cancel an order with status '{order.Status}'.");
        }

        if (order.Status == "Cancelled")
        {
            _logger.LogInformation("Order {OrderId} is already cancelled. Skipping cancellation", orderId);
            return;
        }

        var productIds = order.OrderItems.Select(item => item.ProductId).ToList();
        var productsToUpdate = await _unitOfWork.Products.GetProductsById(productIds);
        var productDict = productsToUpdate.ToDictionary(p => p.Id);

        foreach (var orderItem in order.OrderItems)
        {
            if (productDict.TryGetValue(orderItem.ProductId, out var product))
            {
                product.StockQuantity += orderItem.Quantity;
                _logger.LogInformation("Restored {Quantity} stock for Product {ProductId}. New stock: {NewStock}", orderItem.Quantity, product.Id, product.StockQuantity);
            }
        }

        order.Status = "Cancelled";

        try
        {
            await _unitOfWork.SaveAllChangesAsync();
            _logger.LogInformation("Successfully cancelled Order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Concurrency or DB error while cancelling Order {OrderId}", orderId);
            throw new Exception("The stock for an item in the order was modified by another user. Please try cancelling the order again.", ex);
        }
    }
}