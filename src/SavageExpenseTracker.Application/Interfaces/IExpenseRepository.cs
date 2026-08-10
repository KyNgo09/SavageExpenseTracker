using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IExpenseRepository
    {
        Task<(IEnumerable<Expense> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize);
        Task<Expense?> GetByIdAsync(long id);
        Task AddAsync (Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(long id);
        Task<IEnumerable<Expense>> GetByUserAndDateRangeAsync(IEnumerable<Guid> userIds, DateTime startDate, DateTime endDate);
    }
}