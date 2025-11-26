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
        var productEntity = await  _unitOfWork.Products.GetByIdAsync(productId.ToString());
       
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

    // --- WRITE METHODS ---

    /// <summary>
    /// Creates a new product, saves it, and returns the created product's details.
    /// </summary>
    public async Task<ProductDetailsDto> CreateProductAsync(AddProductDto productDto)
    {
        // 1. Validate (Business Logic)
        if (productDto is null)
            throw new ArgumentNullException(nameof(productDto));
        if (productDto.Price <= 0)
            throw new ArgumentException("Price must be a positive number.", nameof(productDto.Price));

        // 2. Map DTO to Entity
        var newProductEntity = new Product
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            StockQuantity = productDto.StockQuantity,
            CategoryId = productDto.CategoryId
        };

        // 3. Add to Repository (in-memory)
        await  _unitOfWork.Products.AddAsync(newProductEntity);

        // 4. (Your Unit of Work will call SaveChangesAsync() here)
        
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
        var existingProduct = await  _unitOfWork.Products.GetByIdAsync(productId.ToString());

        // 2. Validate it exists
        if (existingProduct is null)
            throw new KeyNotFoundException($"Product with ID {productId} not found.");

        // 3. Update properties
        existingProduct.Name = productDto.Name;
        existingProduct.Description = productDto.Description;
        existingProduct.Price = productDto.Price;
        existingProduct.StockQuantity = productDto.StockQuantity;

        // EF Core's change tracker handles the update.
        // Your Unit of Work will call SaveChangesAsync() to persist the changes.
    }

    /// <summary>
    /// Deletes a product permanently from the database.
    /// </summary>
    public async Task DeleteProductAsync(int productId)
    {
        // 1. Fetch the existing entity
        var productToDelete = await  _unitOfWork.Products.GetByIdAsync(productId.ToString());

        // 2. Validate it exists
        if (productToDelete is null)
            throw new KeyNotFoundException($"Product with ID {productId} not found.");

        // 3. Remove from Repository (in-memory)
         _unitOfWork.Products.Remove(productToDelete);

        // Your Unit of Work will call SaveChangesAsync() to apply the delete.
    }
}