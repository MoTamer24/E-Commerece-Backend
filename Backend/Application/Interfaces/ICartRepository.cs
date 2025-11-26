namespace Application.Interfaces;
using Domain.Entities;

public interface ICartRepository:IGenericRepository<Cart>
{
    Task<Cart?> GetCartByCustomerId(Guid customerId);
}