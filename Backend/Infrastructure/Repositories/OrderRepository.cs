using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order> ,IOrderRepository
{
 
    public OrderRepository(ApplicationDbContext context):base(context)
    {
        
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerId(Guid customerId)
    {
        var orders 
            =await _context.Orders
                .Where(o => o.UserId == customerId).ToListAsync();
        return orders;
    }

    public async Task<Order?> GetOrderDetails(int orderId)
    {
        var order
            = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o=>o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        return order;
    }
    
    
}