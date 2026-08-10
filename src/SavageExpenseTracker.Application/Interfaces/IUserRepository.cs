using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IUserRepository
    {
        // Get Methods
        Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUserNameAsync(string username);
        
        // Create, Update, Delete Methods
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);

        // Exist Methods
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UserNameExistsAsync(string username);

        Task<(IEnumerable<User> Items, int TotalCount)> SearchByEmailAsync(string query, int pageNumber, int pageSize);
    }
}