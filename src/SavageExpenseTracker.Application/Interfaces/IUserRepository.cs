using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.User;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IUserRepository
    {
        // Get Methods
        Task<IEnumerable<User>> GetAllAsync();
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
    }
}