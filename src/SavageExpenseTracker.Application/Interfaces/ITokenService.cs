using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Application.Dtos.User;
using System.Security.Claims;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}