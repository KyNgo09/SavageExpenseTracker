using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<Category?> GetByIdAsync(long id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(long id);
        Task<bool> HasExpensesAsync(long id); // check if category has expenses
    }
}