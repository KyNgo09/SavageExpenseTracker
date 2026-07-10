using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Helpers
{
    public static class ExpenseMappingExtensions
    {
        public static ExpenseDto ToDto(this Expense expense)
        {
            return new ExpenseDto
            {
                Id = expense.Id,
                UserId = expense.UserId,
                Description = expense.Description,
                Amount = expense.Amount,
                ImageUrl = expense.ImageUrl,
                TimeWork = expense.TimeWork,
                SavageComment = expense.SavageComment,
                CreatedAt = expense.CreatedAt
            };
        }
    }
}