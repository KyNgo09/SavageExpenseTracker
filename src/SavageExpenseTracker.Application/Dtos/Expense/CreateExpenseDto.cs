using System;
using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.Expense
{
    public class CreateExpenseDto
    {
        [Required(ErrorMessage = "UserId is required.")]
        public Guid UserId { get; set; }

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "CategoryId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "CategoryId must be valid.")]
        public long CategoryId { get; set; }

        [StringLength(512, ErrorMessage = "ImageUrl cannot exceed 512 characters.")]
        public string? ImageUrl { get; set; }
    }
}