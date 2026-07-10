using System;

namespace SavageExpenseTracker.Domain.Entities
{
    public class Expense
    {
        public long Id { get; set; }
        public Guid UserId { get; set; } // ForeignKey to User
        public string? ImageUrl {get; set; }
        public string? Description {get; set; }
        public decimal Amount { get; set; }
        public decimal TimeWork { get; set; }
        public string? SavageComment { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal AppliedHourlyRate { get; set; }

        public User? User { get; set; }
    }
}