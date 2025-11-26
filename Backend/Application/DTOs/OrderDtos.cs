namespace Application.DTOs;
using Domain.Entities;
using Domain.Entities.Identity;

public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; } // Calculated sum of items and shipping
    public string Status { get; set; } = "Pending"; // e.g., "Pending", "Paid", "Shipped"
}

public class OrderDetailsDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; } // Calculated sum of items and shipping
    public string Status { get; set; } = "Pending"; // e.g., "Pending", "Paid", "Shipped"

    // Shipping Address fields (copied at time of order)
    public string? ShippingStreet { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    
    // Foreign Key
    public Guid UserId { get; set; } // FK to ApplicationUser (uses string ID from Identity)

    // Navigation Properties
    public ApplicationUser User { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } // Links to the order line items
    public Payment Payment { get; set; } // 1:1 relationship with Payment
}