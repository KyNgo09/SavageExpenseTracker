using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.User;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IUserService
    {
        // Get Methods
        Task<PagedResultDto<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto?> GetUserByUserNameAsync(string username);

        // Create, Update, Delete Methods
        Task<UserDto> RegisterUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(Guid id);

        // Exist Methods
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UserNameExistsAsync(string username);

        // Authentication Methods
        Task<UserDto?>AuthenticateAsync(LoginDto loginDto);
        Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changePasswordDto);
        
        // Search Methods
        Task<PagedResultDto<UserDto>> SearchUserByEmailAsync(string query, int pageNumber, int pageSize);
    }
}