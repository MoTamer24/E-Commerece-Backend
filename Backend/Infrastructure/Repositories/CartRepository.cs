using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories;

// Infrastructure/Repositories/CartRepository.cs
public class CartRepository : GenericRepository<Cart>, ICartRepository
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Cart?> GetCartByCustomerId(Guid userId)
    {
        return await _context.Carts.Include(c => c.CartItems)
            .ThenInclude(i => i.Product) // Important: load the product details for each item
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }
}