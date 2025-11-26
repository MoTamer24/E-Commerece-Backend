using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
   
        

        public CategoriesController(ICategoryService categoryService, IUnitOfWork unitOfWork)
        {
            _categoryService = categoryService;
      
        }

        // GET: api/categories
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories =await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(int id)
        {

            var category = await  _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        // POST: api/categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto category)
        {
            await  _categoryService.CreateCategoryAsync(category);
          
            return CreatedAtAction(nameof(GetCategoryById), category);
        }

        // PUT: api/categories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto category)
        {
           
            await  _categoryService.UpdateCategoryAsync(id,category);
         
            return Ok("has been Created");
        }

        // DELETE: api/categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
          await  _categoryService.DeleteCategoryAsync(id);
          return Ok("has been delted");
        }
    }
}
