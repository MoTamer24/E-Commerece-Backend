using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        _logger.LogInformation("Retrieving all categories");
        var categories = await _unitOfWork.Categories.GetAllAsync();
        return categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving category with ID {CategoryId}", id);
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category is null)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found", id);
            return null;
        }
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto)
    {
        _logger.LogInformation("Creating category with Name {CategoryName}", categoryDto.Name);
        var categoryEntity = new Category { Name = categoryDto.Name };
        await _unitOfWork.Categories.AddAsync(categoryEntity);
        await _unitOfWork.SaveAllChangesAsync();
        _logger.LogInformation("Successfully created category {CategoryName} with ID {CategoryId}", categoryEntity.Name, categoryEntity.Id);
        return new CategoryDto { Id = categoryEntity.Id, Name = categoryEntity.Name };
    }

    public async Task UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto)
    {
        _logger.LogInformation("Updating category with ID {CategoryId}", id);
        var categoryEntity = await _unitOfWork.Categories.GetByIdAsync(id);
        if (categoryEntity is null)
        {
            _logger.LogWarning("Update failed: Category with ID {CategoryId} not found", id);
            throw new KeyNotFoundException("Category not found.");
        }

        categoryEntity.Name = categoryDto.Name;
        await _unitOfWork.SaveAllChangesAsync();
        _logger.LogInformation("Successfully updated category with ID {CategoryId} to Name {CategoryName}", id, categoryDto.Name);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("Deleting category with ID {CategoryId}", id);
        var categoryEntity = await _unitOfWork.Categories.GetByIdAsync(id);
        if (categoryEntity is null)
        {
            _logger.LogWarning("Delete failed: Category with ID {CategoryId} not found", id);
            throw new KeyNotFoundException("Category not found.");
        }

        _unitOfWork.Categories.Remove(categoryEntity);
        await _unitOfWork.SaveAllChangesAsync();
        _logger.LogInformation("Successfully deleted category with ID {CategoryId}", id);
    }
}
