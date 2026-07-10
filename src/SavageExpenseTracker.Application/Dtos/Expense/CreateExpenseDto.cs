using System;

namespace SavageExpenseTracker.Application.Dtos.Expense
{
    public class CreateExpenseDto
    {
        public Guid UserId { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
       // ImageUrl, SavageComment and TimeWork will be processed automatically
    }
}