using System;

namespace SavageExpenseTracker.Application.Dtos.Expense
{
    public class CreateExpenseDto
    {
        public Guid UserId { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public long CategoryId { get; set; }
        public string? ImageUrl { get; set; }
    }
}