namespace Application.Interfaces.Services;
using Application.DTOs;

public interface IProductService
{
    /// <summary>
    /// Gets a list of products for public display in the catalog.
    /// </summary>
    /// <returns>A collection of products with basic catalog information.</returns>
    Task<IEnumerable<ProductCatalogDto>> GetProductsForCatalogAsync();
    Task<IEnumerable<ProductCatalogDto>> GetAll();

    /// <summary>
    /// Gets a single product's detailed information by its ID.
    /// </summary>
    /// <param name="productId">The ID of the product to retrieve.</param>
    /// <returns>The detailed product DTO, or null if not found.</returns>
    Task<ProductDetailsDto?> GetProductByIdAsync(int productId);

    /// <summary>
    /// Creates a new product based on the provided data. (Admin)
    /// </summary>
    /// <param name="productDto">The DTO containing the new product's data.</param>
    /// <returns>A DTO of the newly created product, including its generated ID.</returns>
    Task<ProductDetailsDto> CreateProductAsync(AddProductDto productDto);

    /// <summary>
    /// Updates an existing product's information. (Admin)
    /// </summary>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="productDto">The DTO containing the updated data.</param>
    Task UpdateProductAsync(int productId, UpdateProductDto productDto);

    /// <summary>
    /// Deletes a product. This might be a soft delete (marking as inactive). (Admin)
    /// </summary>
    /// <param name="productId">The ID of the product to delete.</param>
    Task DeleteProductAsync(int productId);
}