using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.Category
{
    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}