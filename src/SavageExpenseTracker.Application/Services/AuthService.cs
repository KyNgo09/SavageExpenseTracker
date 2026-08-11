using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<UserDto?> AuthenticateAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return null;
            }

            if (!_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }
            
            return user.ToDto();
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null || !_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract principal from expired access token during refresh token operation.");
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

            if (!_passwordHasher.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Incorrect old password!");
            }

            user.PasswordHash = _passwordHasher.HashPassword(changePasswordDto.NewPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiryDate = null;
            
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}
