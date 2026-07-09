using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Helpers
{
    public static class UserMappingExtensions
    {
        public static UserDto ToDto(this User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                HourlyRate = user.HourlyRate,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
