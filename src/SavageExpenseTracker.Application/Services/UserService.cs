using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Domain.Entities;


namespace SavageExpenseTracker.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        
        public async Task<IEnumerable<UserDto>> GetAllUserAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => u.ToDto());
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : user.ToDto();
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : user.ToDto();
        }

        public async Task<UserDto?> GetUserByUserNameAsync(string username)
        {
            var user = await _userRepository.GetByUserNameAsync(username);
            return user == null ? null : user.ToDto();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userRepository.EmailExistsAsync(email);
        }

        public async Task<UserDto> RegisterUserAsync(CreateUserDto createUserDto)
        {
            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
            {
                throw new InvalidOperationException("Email already exists!");
            }

            var passwordHash = PasswordHasher.HashPassword(createUserDto.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = createUserDto.Email,
                Username = createUserDto.Username,
                PasswordHash = passwordHash,
                HourlyRate = createUserDto.HourlyRate,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return user.ToDto();
        }

        public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.Username = updateUserDto.Username;
            user.HourlyRate = updateUserDto.HourlyRate;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return true;
        }

        public async Task<UserDto?> AuthenticateAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return null;
            }

            var hashInput = PasswordHasher.HashPassword(loginDto.Password);
            if (user.PasswordHash != hashInput)
            {
                return null;
            }
            
            return user.ToDto();
        }

        public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changePasswordDto)
        {
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
            {
                throw new InvalidOperationException("New password and confirm new password do not match!");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new InvalidOperationException("User not found!");
            }

            var oldHash = PasswordHasher.HashPassword(changePasswordDto.OldPassword);
            if (user.PasswordHash != oldHash)
            {
                throw new InvalidOperationException("Incorrect old password!");
            }

            user.PasswordHash = PasswordHasher.HashPassword(changePasswordDto.NewPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<IEnumerable<UserDto>> SearchUserByEmailAsync(string query)
        {
            var allUsers = await _userRepository.GetAllAsync();
            var matchedUsers = allUsers.Where(u => u.Email.Contains(query, StringComparison.OrdinalIgnoreCase));
            return matchedUsers.Select(u => u.ToDto());
        }
    }
}