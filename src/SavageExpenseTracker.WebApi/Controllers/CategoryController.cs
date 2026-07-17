using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Dtos.Category;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto?>> GetById(long id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound(new { Message = "Category Not Found" });

            return Ok(category);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto createCategoryDto)
        {
            try
            {
                var created = await _categoryService.CreateCategoryAsync(createCategoryDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch(Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                var result = await _categoryService.UpdateCategoryAsync(id, updateCategoryDto);
                if(!result) return NotFound(new { Message = "Category Not Found" });
                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _categoryService.DeleteCategoryAsync(id);
                if(!result) return NotFound(new { Message = "Category Not Found" });
                return NoContent();
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}