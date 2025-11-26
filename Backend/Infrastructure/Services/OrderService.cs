
using Application.Interfaces.Services;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Services;

public class OrderService : IOrderService
{
   
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
     
        IUnitOfWork unitOfWork)
    {
       
        _unitOfWork = unitOfWork;
    }
       public async Task<OrderSummaryDto> CreateOrderAsync(Guid customerId)
    {
        // load cart items 
       // --- The parts you did perfectly ---

// 1. Load cart items
var customerCartItems = (await _unitOfWork.Carts.GetCartByCustomerId(customerId)).CartItems;

// 2. Get all product IDs from the cart (using a cleaner LINQ method)
var productIds = customerCartItems.Select(item => item.ProductId).ToList();

// 3. Get all product info in ONE database call (excellent optimization!)
var productsFromDb = await  _unitOfWork.Products.GetProductsById(productIds);

// --- The corrected loop logic ---

// Convert to a Dictionary for easy lookup. This is the key fix!
var productDict = productsFromDb.ToDictionary(p => p.Id);

decimal totalAmount = 0;
var orderItems = new List<OrderItem>();

foreach (var cartItem in customerCartItems)
{
    // A. Find the product safely
    if (!productDict.TryGetValue(cartItem.ProductId, out var product))
    {
        // This product doesn't exist in the DB, which is a serious issue.
        throw new InvalidOperationException($"Product with ID {cartItem.ProductId} not found.");
    }

    // B. Check the stock correctly
    if (product.StockQuantity < cartItem.Quantity)
    {
        throw new InvalidOperationException($"Not enough stock for product: {product.Name}. Available: {product.StockQuantity}, Requested: {cartItem.Quantity}");
    }

    // C. Calculate the total correctly (using PRICE, not stock)
    totalAmount += product.Price * cartItem.Quantity;

    // D. Reduce the stock in memory
    product.StockQuantity -= cartItem.Quantity;
    
    // E. Create the OrderItem for the new Order
    orderItems.Add(new OrderItem
    {
        ProductId = product.Id,
        Quantity = cartItem.Quantity,
        PriceAtPurchase = product.Price // Lock in the price at the time of purchase!
    });
    
    
}

// --- Next steps would be to create the Order and save ---
var newOrder = new Order
{
    UserId = customerId,
    OrderDate = DateTime.UtcNow,
    TotalAmount = totalAmount,
    Status = "Pending Payment",
    OrderItems = orderItems
};

await _unitOfWork.Orders.AddAsync(newOrder);

// The ConcurrencyCheck will happen here when you save!
        try
        {
            await _unitOfWork.SaveAllChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("DB updates happened", ex);
        }

        return new OrderSummaryDto()
{
    OrderId= newOrder.Id,
    OrderDate = newOrder.OrderDate,
    Status = newOrder.Status,
    TotalAmount = newOrder.TotalAmount
};
    }

    /// <summary>
    /// Gets a list of all orders for a specific user.
    /// (You had this one: GetUserOrders - excellent!)
    /// </summary>
    /// <param name="customerId">The ID of the customer.</param>
    public async Task<IEnumerable<OrderSummaryDto>> GetOrdersForCustomerAsync(Guid customerId)
    {
      
       var orders= await  _unitOfWork.Orders.GetOrdersByCustomerId(customerId);
       return orders.Select(order => new OrderSummaryDto
       {
           OrderDate = order.OrderDate,
           Status = order.Status,
           TotalAmount = order.TotalAmount
       });
    }


    /// <summary>
    /// Gets the full details of a single order.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(int orderId)
    { 
        var order =await  _unitOfWork.Orders.GetOrderDetails(orderId);
        if (order == null)
        {
            throw new Exception("no Order with this Id");
        }
        var orderDetails = new OrderDetailsDto()
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


    /// <summary>
    /// Updates an order's status. For example, after a successful payment
    /// you would call this to change the status from "Pending Payment" to "Processing".
    /// (You had this one: changeStatus - perfect!)
    /// </summary>
    /// <param name="orderId">The ID of the order to update.</param>
    /// <param name="newStatus">The new status string.</param>
    public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var order =await  _unitOfWork.Orders.GetByIdAsync(orderId.ToString());
        if (order == null)
        {
            throw new Exception("no Order with this Id");
        }
        
        order.Status = newStatus;
        try
        {
            await _unitOfWork.SaveAllChangesAsync();
        }
        catch(Exception ex)
        {
            throw new Exception("Error updating order status ", ex);
        }
    }

    /// <summary>
    /// Cancels an order and releases the reserved stock.
    /// </summary>
    /// <param name="orderId">The ID of the order to cancel.</param>
    public async Task CancelOrderAsync(int orderId)
    {
        // STEP 1: Get the complete order, including its items.
        // We need a specific repository method for this to ensure OrderItems are loaded.
        var order = await  _unitOfWork.Orders.GetOrderDetails(orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        // STEP 2: Apply Business Logic. You can't cancel an order that's already shipped.
        if (order.Status == "Shipped" || order.Status == "Completed")
        {
            throw new InvalidOperationException($"Cannot cancel an order with status '{order.Status}'.");
        }
        
        // This makes the operation safe to call multiple times.
        if (order.Status == "Cancelled")
        {
            // The order is already cancelled, do nothing.
            return;
        }

        // STEP 3: Restore the stock for each product in the order.
        // This logic is the reverse of the CreateOrderAsync method.
        var productIds = order.OrderItems.Select(item => item.ProductId).ToList();
        var productsToUpdate = await  _unitOfWork.Products.GetProductsById(productIds);
        var productDict = productsToUpdate.ToDictionary(p => p.Id);

        foreach (var orderItem in order.OrderItems)
        {
            if (productDict.TryGetValue(orderItem.ProductId, out var product))
            {
                // Add the quantity back to the product's stock.
                product.StockQuantity += orderItem.Quantity;
            }
            // Note: If a product was deleted, we might choose to ignore it or log an error,
            // but for now, we just update the ones we find.
        }

        // STEP 4: Update the order's status.
        order.Status = "Cancelled";

        // STEP 5: Save all changes in a single transaction.
        // The Unit of Work will automatically create UPDATE statements for the products'
        // stock AND the order's status and commit them together.
        try
        {
            await _unitOfWork.SaveAllChangesAsync();
        }
        catch (Exception ex)
        {
            // This could happen if, for example, an admin was manually editing the product
            // at the exact same moment. We need to inform the user to try again.
            throw new Exception("The stock for an item in the order was modified by another user. Please try cancelling the order again.", ex);
        }
        
    }


    
}