using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    [ConcurrencyCheck]
    public decimal Price { get; set; } // Use decimal for currency
    [ConcurrencyCheck]
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    
    // Foreign Key
    public int CategoryId { get; set; }

    // Navigation Properties
    public Category Category { get; set; }
    public ICollection<Review> Reviews { get; set; }

}