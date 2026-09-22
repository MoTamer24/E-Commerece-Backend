namespace Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICartRepository Carts { get; }
    ICategoryRepository Categories { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }
    IProductRepository Products { get; }
    IReviewRepository Reviews { get; }

    Task<int> SaveAllChangesAsync();
}