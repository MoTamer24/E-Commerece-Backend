using Application.Interfaces;
using Infrastructure.Repositories;
namespace Infrastructure;

public class UnitOfWork:IUnitOfWork
{
   private readonly ApplicationDbContext _context;

    // Backing fields for the repositories
    private ICartRepository _carts;
    private ICategoryRepository _categories;
    private IOrderRepository _orders;
    private IPaymentRepository _payments;
    private IProductRepository _products;
    private IReviewRepository _reviews;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }
   public ICartRepository Carts => _carts ??= new CartRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);

    public async Task<int> SaveAllChangesAsync()
    {
         return await _context.SaveChangesAsync();
    }
    public void Dispose()
    {
        _context.Dispose();
    }
    
}