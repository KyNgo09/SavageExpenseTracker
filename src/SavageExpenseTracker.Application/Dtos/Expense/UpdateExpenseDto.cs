using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.Expense
{
    public class UpdateExpenseDto
    {
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "CategoryId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "CategoryId must be valid.")]
        public long CategoryId { get; set; }
    }
}