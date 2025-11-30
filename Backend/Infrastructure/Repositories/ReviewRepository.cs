using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Review>> GetReviewsForProductAsync(int productId)
    {
        return await _context.Reviews.Include(r=>r.User).Include(r=>r.Product)
            .Where(r => r.ProductId == productId)
            .ToListAsync();
    }
}