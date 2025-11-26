using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Infrastructure.Services;

public class CategoryService : ICategoryService
{

    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork=unitOfWork;
      
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await  _unitOfWork.Categories.GetAllAsync();
        return categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await  _unitOfWork.Categories.GetByIdAsync(id.ToString());
        if (category is null) return null;
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto)
    {
        var categoryEntity = new Category { Name = categoryDto.Name };
        await  _unitOfWork.Categories.AddAsync(categoryEntity);
         await _unitOfWork.SaveAllChangesAsync();
        return new CategoryDto { Id = categoryEntity.Id, Name = categoryEntity.Name };
    }

    public async Task UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto)
    {
        var categoryEntity = await  _unitOfWork.Categories.GetByIdAsync(id.ToString());
        if (categoryEntity is null)
            throw new KeyNotFoundException("Category not found.");

        categoryEntity.Name = categoryDto.Name;
       await _unitOfWork.SaveAllChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var categoryEntity = await  _unitOfWork.Categories.GetByIdAsync(id.ToString());
        if (categoryEntity is null)
            throw new KeyNotFoundException("Category not found.");

         _unitOfWork.Categories.Remove(categoryEntity);
               await _unitOfWork.SaveAllChangesAsync();
    }
}
