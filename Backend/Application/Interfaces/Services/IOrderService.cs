using Application.DTOs;

namespace Application.Interfaces.Services;
using Domain.Entities;

public interface IOrderService
{
    /// <summary>
    /// The main "checkout" use case. It handles the entire process:
    /// validating the cart, checking stock, creating a pending order, and reserving items.
    /// This ONE method will internally do the "load", "check stock", and "calculate amount" steps you listed.
    /// </summary>
    /// <param name="customerId">The ID of the customer placing the order.</param>
    /// <returns>A summary of the newly created, pending order.</returns>
    Task<OrderSummaryDto> CreateOrderAsync(Guid customerId);

    /// <summary>
    /// Gets a list of all orders for a specific user.
    /// (You had this one: GetUserOrders - excellent!)
    /// </summary>
    /// <param name="customerId">The ID of the customer.</param>
    Task<IEnumerable<OrderSummaryDto>> GetOrdersForCustomerAsync(Guid customerId);
    
    /// <summary>
    /// Gets the full details of a single order.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    Task<OrderDetailsDto?> GetOrderDetailsAsync(int orderId);

    /// <summary>
    /// Updates an order's status. For example, after a successful payment
    /// you would call this to change the status from "Pending Payment" to "Processing".
    /// (You had this one: changeStatus - perfect!)
    /// </summary>
    /// <param name="orderId">The ID of the order to update.</param>
    /// <param name="newStatus">The new status string.</param>
    Task UpdateOrderStatusAsync(int orderId, string newStatus);
    
    /// <summary>
    /// Cancels an order and releases the reserved stock.
    /// </summary>
    /// <param name="orderId">The ID of the order to cancel.</param>
    Task CancelOrderAsync(int orderId);



}