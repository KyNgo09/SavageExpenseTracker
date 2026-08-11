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
        Task<UserDto?> UpdateAvatarAsync(Guid userId, string avatarUrl);
        Task<UserDto?> UploadAvatarAsync(Guid userId, System.IO.Stream stream, string fileName, string contentType, long fileLength);
        Task<bool> DeleteUserAsync(Guid id);

        // Exist Methods
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UserNameExistsAsync(string username);
        
        // Search Methods
        Task<PagedResultDto<UserDto>> SearchUserByEmailAsync(string query, int pageNumber, int pageSize);
    }
}