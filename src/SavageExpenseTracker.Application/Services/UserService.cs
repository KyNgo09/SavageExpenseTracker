using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private readonly ITokenService _tokenService;
        private readonly IPhotoService _photoService;

        public UserService(IUserRepository userRepository, ITokenService tokenService, IPhotoService photoService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _photoService = photoService;
        }
        
        public async Task<PagedResultDto<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

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

            var passwordHash = PasswordHasher.HashPassword(createUserDto.Password);

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

        public async Task<UserDto?> AuthenticateAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return null;
            }

            if (!PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }
            
            return user.ToDto();
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null || !PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(TokenApiModel tokenApiModel)
        {
            if (tokenApiModel == null || string.IsNullOrEmpty(tokenApiModel.AccessToken) || string.IsNullOrEmpty(tokenApiModel.RefreshToken))
            {
                return null;
            }

            ClaimsPrincipal principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(tokenApiModel.AccessToken);
            }
            catch
            {
                return null;
            }

            var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return null;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.RefreshToken != tokenApiModel.RefreshToken || user.RefreshTokenExpiryDate <= DateTime.UtcNow)
            {
                return null;
            }

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
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

            if (!PasswordHasher.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Incorrect old password!");
            }

            user.PasswordHash = PasswordHasher.HashPassword(changePasswordDto.NewPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiryDate = null;
            
            await _userRepository.UpdateAsync(user);
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
            if (stream == null || fileLength == 0)
                throw new InvalidOperationException("Please select an image file for avatar!");

            if (fileLength > 5 * 1024 * 1024)
                throw new InvalidOperationException("Avatar file size must not exceed 5 MB!");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension) || !contentType.StartsWith("image/"))
            {
                throw new InvalidOperationException("Invalid image file format! Only JPG, JPEG, PNG, and WEBP images are allowed.");
            }

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

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

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