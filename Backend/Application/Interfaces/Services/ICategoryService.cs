using Application.DTOs;

namespace Application.Interfaces.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto);
    Task UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto);
    Task DeleteCategoryAsync(int id);
    
}