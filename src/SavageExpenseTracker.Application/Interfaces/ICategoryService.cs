using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Category;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<PagedResultDto<CategoryDto>> GetCategoriesPagedAsync(int pageNumber, int pageSize);
        Task<CategoryDto?> GetCategoryByIdAsync(long id);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
        Task<bool> UpdateCategoryAsync(long id, UpdateCategoryDto updateCategoryDto);
        Task<bool> DeleteCategoryAsync(long id);
    }
}