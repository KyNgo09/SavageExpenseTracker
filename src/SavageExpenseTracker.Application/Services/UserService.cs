using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Constants;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoService _photoService;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(
            IUserRepository userRepository, 
            IPhotoService photoService,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _photoService = photoService;
            _passwordHasher = passwordHasher;
        }
        
        public async Task<PagedResultDto<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var (items, totalCount) = await _userRepository.GetAllAsync(pageNumber, pageSize);

            return new PagedResultDto<UserDto>
            {
                Items = items.Select(u => u.ToDto()),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

        public async Task<bool> UserNameExistsAsync(string username)
        {
            return await _userRepository.UserNameExistsAsync(username);
        }

        public async Task<UserDto> RegisterUserAsync(CreateUserDto createUserDto)
        {
            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
            {
                throw new InvalidOperationException("Email already exists!");
            }

            if (await _userRepository.UserNameExistsAsync(createUserDto.UserName))
            {
                throw new InvalidOperationException("Username already exists!");
            }

            var passwordHash = _passwordHasher.HashPassword(createUserDto.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = createUserDto.Email,
                UserName = createUserDto.UserName,
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

            if (user.UserName != updateUserDto.UserName && await _userRepository.UserNameExistsAsync(updateUserDto.UserName))
            {
                throw new InvalidOperationException("Username already exists!");
            }

            user.UserName = updateUserDto.UserName;
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

        public async Task<UserDto?> UpdateAvatarAsync(Guid userId, string avatarUrl)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            user.AvatarUrl = avatarUrl;
            await _userRepository.UpdateAsync(user);

            return user.ToDto();
        }

        public async Task<UserDto?> UploadAvatarAsync(Guid userId, System.IO.Stream stream, string fileName, string contentType, long fileLength)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            FileUploadHelper.ValidateImageFile(stream, fileName, contentType, fileLength, 5 * 1024 * 1024, allowedExtensions);

            var avatarUrl = await _photoService.UploadPhotoAsync(stream, fileName, PhotoFolders.UserAvatars);
            return await UpdateAvatarAsync(userId, avatarUrl);
        }

        public async Task<PagedResultDto<UserDto>> SearchUserByEmailAsync(string query, int pageNumber, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new PagedResultDto<UserDto>
                {
                    Items = Enumerable.Empty<UserDto>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var (items, totalCount) = await _userRepository.SearchByEmailAsync(query, pageNumber, pageSize);

            return new PagedResultDto<UserDto>
            {
                Items = items.Select(u => u.ToDto()),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}