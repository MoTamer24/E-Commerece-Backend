namespace Infrastructure.Services;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Application.DTOs;
using Microsoft.Extensions.Logging;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(IUnitOfWork unitOfWork, ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task AddToCartAsync(Guid userId, int productId, int quantity)
    {
        _logger.LogInformation("Adding product {ProductId} with quantity {Quantity} to cart for User {UserId}", productId, quantity, userId);

        var cart = await _unitOfWork.Carts.GetCartByCustomerId(userId);
        if (cart == null)
        {
            _logger.LogInformation("Creating new cart for User {UserId}", userId);
            cart = new Cart { UserId = userId };
            await _unitOfWork.Carts.AddAsync(cart);
        }

        var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
            _logger.LogInformation("Updated quantity of product {ProductId} to {Quantity} for User {UserId}", productId, cartItem.Quantity, userId);
        }
        else
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Failed to add to cart: Product {ProductId} not found", productId);
                throw new Exception("Product not found");
            }

            cart.CartItems.Add(new CartItem { ProductId = productId, Quantity = quantity });
            _logger.LogInformation("Added new product {ProductId} to cart for User {UserId}", productId, userId);
        }

        await _unitOfWork.SaveAllChangesAsync();
        _logger.LogInformation("Successfully saved cart changes for User {UserId}", userId);
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        _logger.LogInformation("Fetching cart for User {UserId}", userId);

        var cart = await _unitOfWork.Carts.GetCartByCustomerId(userId);
        if (cart == null)
        {
            _logger.LogInformation("No cart found for User {UserId}, returning empty cart", userId);
            return new CartDto();
        }

        var cartDto = new CartDto
        {
            Items = cart.CartItems.Select(item => new CartItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Price = item.Product.Price,
                Quantity = item.Quantity
            }).ToList()
        };

        return cartDto;
    }

    public async Task RemoveFromCartAsync(Guid userId, int productId)
    {
        _logger.LogInformation("Removing product {ProductId} from cart for User {UserId}", productId, userId);

        var cart = await _unitOfWork.Carts.GetCartByCustomerId(userId);
        if (cart == null)
        {
            _logger.LogWarning("Cannot remove product {ProductId}: Cart not found for User {UserId}", productId, userId);
            return;
        }

        var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
        if (cartItem != null)
        {
            cart.CartItems.Remove(cartItem);
            await _unitOfWork.SaveAllChangesAsync();
            _logger.LogInformation("Successfully removed product {ProductId} from cart for User {UserId}", productId, userId);
        }
        else
        {
            _logger.LogWarning("Product {ProductId} was not found in cart for User {UserId}", productId, userId);
        }
    }
}