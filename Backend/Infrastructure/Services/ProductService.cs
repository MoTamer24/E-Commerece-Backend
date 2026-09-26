using Application.Interfaces;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductCatalogDto>> GetAll()
    {
        _logger.LogInformation("Retrieving all products");
        var products = await _unitOfWork.Products.GetAllAsync();
        return products.Select(p => new ProductCatalogDto
        {
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            StockQuantity = p.StockQuantity
        });
    }

    public async Task<IEnumerable<ProductCatalogDto>> GetProductsForCatalogAsync()
    {
        _logger.LogInformation("Retrieving products for catalog");
        var productEntities = await _unitOfWork.Products.GetAllAsync();
        return productEntities.Select(p => new ProductCatalogDto
        {
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl
        });
    }

    public async Task<ProductDetailsDto?> GetProductByIdAsync(int productId)
    {
        _logger.LogInformation("Retrieving product details for Product {ProductId}", productId);
        var productEntity = await _unitOfWork.Products.GetByIdAsync(productId);

        if (productEntity is null)
        {
            _logger.LogWarning("Product {ProductId} not found", productId);
            return null;
        }

        return new ProductDetailsDto
        {
            id = productEntity.Id,
            Name = productEntity.Name,
            Price = productEntity.Price,
            Description = productEntity.Description,
            StockQuantity = productEntity.StockQuantity,
            categoryName = productEntity.Category?.Name
        };
    }

    public async Task<ProductDetailsDto> CreateProductAsync(AddProductDto productDto)
    {
        if (productDto is null)
        {
            _logger.LogWarning("Failed to create product: Product DTO is null");
            throw new ArgumentNullException(nameof(productDto));
        }

        if (productDto.Price <= 0)
        {
            _logger.LogWarning("Failed to create product {ProductName}: Price {Price} must be positive", productDto.Name, productDto.Price);
            throw new ArgumentException("Price must be a positive number.", nameof(productDto.Price));
        }

        _logger.LogInformation("Creating product {ProductName} in Category {CategoryId}", productDto.Name, productDto.CategoryId);

        var newProductEntity = new Product
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            StockQuantity = productDto.StockQuantity,
            CategoryId = productDto.CategoryId
        };

        await _unitOfWork.Products.AddAsync(newProductEntity);
        await _unitOfWork.SaveAllChangesAsync();

        _logger.LogInformation("Successfully created product {ProductName} with ID {ProductId}", newProductEntity.Name, newProductEntity.Id);

        return new ProductDetailsDto
        {
            id = newProductEntity.Id,
            Name = newProductEntity.Name,
            Description = newProductEntity.Description,
            Price = newProductEntity.Price,
            StockQuantity = newProductEntity.StockQuantity
        };
    }

    public async Task UpdateProductAsync(int productId, UpdateProductDto productDto)
    {
        _logger.LogInformation("Updating product with ID {ProductId}", productId);
        var existingProduct = await _unitOfWork.Products.GetByIdAsync(productId);

        if (existingProduct is null)
        {
            _logger.LogWarning("Failed to update product: Product {ProductId} not found", productId);
            throw new KeyNotFoundException($"Product with ID {productId} not found.");
        }

        existingProduct.Name = productDto.Name;
        existingProduct.Description = productDto.Description;
        existingProduct.Price = productDto.Price;
        existingProduct.StockQuantity = productDto.StockQuantity;

        await _unitOfWork.SaveAllChangesAsync();
        _logger.LogInformation("Successfully updated product with ID {ProductId}", productId);
    }

    public async Task DeleteProductAsync(int productId)
    {
        _logger.LogInformation("Deleting product with ID {ProductId}", productId);
        var productToDelete = await _unitOfWork.Products.GetByIdAsync(productId);

        if (productToDelete is null)
        {
            _logger.LogWarning("Failed to delete product: Product {ProductId} not found", productId);
            throw new KeyNotFoundException($"Product with ID {productId} not found.");
        }

        _unitOfWork.Products.Remove(productToDelete);
        await _unitOfWork.SaveAllChangesAsync();
        _logger.LogInformation("Successfully deleted product with ID {ProductId}", productId);
    }
}