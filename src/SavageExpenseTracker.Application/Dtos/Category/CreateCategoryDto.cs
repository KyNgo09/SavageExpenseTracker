using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.Category
{
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }
}