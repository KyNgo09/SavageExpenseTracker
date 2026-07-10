using SavageExpenseTracker.Application.Dtos.Category;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Helpers
{
    public static class CategoryMappingExtensions
    {
        public static CategoryDto ToDto(this Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                CreatedAt = category.CreatedAt
            };
        }
    }
}