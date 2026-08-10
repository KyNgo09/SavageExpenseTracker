using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Category;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<PagedResultDto<CategoryDto>> GetAllCategoriesAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _categoryRepository.GetPagedAsync(pageNumber, pageSize);

            return new PagedResultDto<CategoryDto>
            {
                Items = items.Select(c => c.ToDto()),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(long id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return category?.ToDto();
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            var category = new Category
            {
                Name = createCategoryDto.Name,
                CreatedAt = DateTime.UtcNow
            };
            await _categoryRepository.AddAsync(category);
            return category.ToDto();
        }

        public async Task<bool> UpdateCategoryAsync(long id, UpdateCategoryDto updateCategoryDto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return false;

            category.Name = updateCategoryDto.Name;
            await _categoryRepository.UpdateAsync(category);
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(long id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return false;

            var hasExpenses = await _categoryRepository.HasExpensesAsync(id);
            if (hasExpenses)
            {
                throw new InvalidOperationException("Category has expenses and cannot be deleted.");
            }

            await _categoryRepository.DeleteAsync(id);
            return true;
        }
    }
}