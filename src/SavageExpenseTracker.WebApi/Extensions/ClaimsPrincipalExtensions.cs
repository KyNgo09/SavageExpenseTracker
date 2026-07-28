using System;
using System.Security.Claims;

namespace SavageExpenseTracker.WebApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Unable to determine current user.");
        }
    }
}