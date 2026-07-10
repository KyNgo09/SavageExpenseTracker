using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IExpenseRepository
    {
        Task<IEnumerable<Expense>> GetByUserIdAsync(Guid userId);
        Task<Expense?> GetByIdAsync(long id);
        Task AddAsync (Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(long id);
    }
}