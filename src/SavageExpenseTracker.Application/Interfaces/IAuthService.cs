using System;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.User;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> AuthenticateAsync(LoginDto loginDto);
        Task<TokenResponseDto?> LoginAsync(LoginDto loginDto);
        Task<TokenResponseDto?> RefreshTokenAsync(TokenApiModel tokenApiModel);
        Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changePasswordDto);
    }
}
