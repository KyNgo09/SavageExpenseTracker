using System;

namespace SavageExpenseTracker.Domain.Entities 
{
    public class User 
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? AvatarUrl { get; set; }
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

        // Auth
        public string Role { get; set; } = "User";

        // Refresh Token
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryDate { get; set; }
    }
}