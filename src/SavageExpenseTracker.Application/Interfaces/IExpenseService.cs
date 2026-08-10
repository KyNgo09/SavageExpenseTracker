using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Expense;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<PagedResultDto<ExpenseDto>> GetUserExpensesAsync(Guid userId, int pageNumber, int pageSize);
        Task<ExpenseDto?> GetExpenseByIdAsync(long id);
        Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto createExpenseDto);
        Task<bool> UpdateExpenseAsync(long id, Guid currentUserId, UpdateExpenseDto updateExpenseDto);
        Task<bool> DeleteExpenseAsync(long id, Guid currentUserId);
    }
}