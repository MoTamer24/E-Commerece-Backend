namespace Application.Interfaces.Services;
using Application.DTOs;
public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task AddToCartAsync(Guid userId, int productId, int quantity);
    Task RemoveFromCartAsync(Guid userId, int productId);
}