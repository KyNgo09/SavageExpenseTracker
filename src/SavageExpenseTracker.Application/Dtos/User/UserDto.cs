using System;

namespace SavageExpenseTracker.Application.Dtos.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}