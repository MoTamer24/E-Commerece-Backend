using Application.Interfaces;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities; 

namespace Infrastructure.Services;

public class ProductService : IProductService
{
     private readonly IUnitOfWork _unitOfWork;

    public ProductService( IUnitOfWork unitOfWork)
    {
        
         _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<ProductCatalogDto>> GetAll()
    {
       var products=  await _unitOfWork.Products.GetAllAsync();
       return products.Select(p=>new ProductCatalogDto()
       {
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            StockQuantity=p.StockQuantity
       });
    }

    // --- READ METHODS ---
    public async Task<IEnumerable<ProductCatalogDto>> GetProductsForCatalogAsync()
    {
        var productEntities = await  _unitOfWork.Products.GetAllAsync();
   
        return productEntities.Select(p => new ProductCatalogDto()
        {
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl
        });
    }

    public async Task<ProductDetailsDto?> GetProductByIdAsync(int productId)
    {
        // For this method, we can use the specific repository method if it includes related data
        var productEntity = await  _unitOfWork.Products.GetByIdAsync(productId);
       
        if (productEntity is null)
        {
            return null;
        }

        return new ProductDetailsDto() 
        {
            id = productEntity.Id,
            Name = productEntity.Name,
            Price = productEntity.Price,
            Description = productEntity.Description,
            StockQuantity = productEntity.StockQuantity,
            categoryName = productEntity.Category?.Name // Safely access related data
        };
    }

 

    /// <summary>
    /// Creates a new product, saves it, and returns the created product's details.
    /// </summary>
    public async Task<ProductDetailsDto> CreateProductAsync(AddProductDto productDto)
    {

        if (productDto is null)
            throw new ArgumentNullException(nameof(productDto));
        if (productDto.Price <= 0)
            throw new ArgumentException("Price must be a positive number.", nameof(productDto.Price));

        var newProductEntity = new Product
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            StockQuantity = productDto.StockQuantity,
            CategoryId = productDto.CategoryId
        };
        await  _unitOfWork.Products.AddAsync(newProductEntity);

        await _unitOfWork.SaveAllChangesAsync();
        
        // 5. Map the created entity (now with an ID) back to a DTO and return it
        return new ProductDetailsDto
        {
            id = newProductEntity.Id,
            Name = newProductEntity.Name,
            Description = newProductEntity.Description,
            Price = newProductEntity.Price,
            StockQuantity = newProductEntity.StockQuantity
        };
    }

    /// <summary>
    /// Updates an existing product's information.
    /// </summary>
    public async Task UpdateProductAsync(int productId, UpdateProductDto productDto)
    {
        // 1. Fetch the existing entity
        var existingProduct = await  _unitOfWork.Products.GetByIdAsync(productId);

        // 2. Validate it exists
        if (existingProduct is null)
            throw new KeyNotFoundException($"Product with ID {productId} not found.");

        // 3. Update properties
        existingProduct.Name = productDto.Name;
        existingProduct.Description = productDto.Description;
        existingProduct.Price = productDto.Price;
        existingProduct.StockQuantity = productDto.StockQuantity;

        
        await _unitOfWork.SaveAllChangesAsync();
    }

    /// <summary>
    /// Deletes a product permanently from the database.
    /// </summary>
    public async Task DeleteProductAsync(int productId)
    {
        // 1. Fetch the existing entity
        var productToDelete = await  _unitOfWork.Products.GetByIdAsync(productId);

        // 2. Validate it exists
        if (productToDelete is null)
            throw new KeyNotFoundException($"Product with ID {productId} not found.");

        // 3. Remove from Repository (in-memory)
         _unitOfWork.Products.Remove(productToDelete);

        
        await _unitOfWork.SaveAllChangesAsync();
    }
}