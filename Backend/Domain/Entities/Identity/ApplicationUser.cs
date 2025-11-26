namespace Domain.Entities.Identity; 
using Microsoft.AspNetCore.Identity;
public  class ApplicationUser:IdentityUser<Guid>  
{
    // Extended fields from the Users table
    // Email and Phone Number are already in IdentityUser
    
    // Address fields (renamed from your ERD 'address')
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }

    // Navigation Property: Orders placed by this user
    public ICollection<Order>? Orders { get; set; }
    
    // Navigation Property: The user's active shopping cart
    public Cart? Cart { get; set; }
    
    // Navigation Property: Reviews submitted by this user
    public ICollection<Review>? Reviews { get; set; }
}