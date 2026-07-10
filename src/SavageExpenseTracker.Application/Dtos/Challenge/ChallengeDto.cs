using System;

namespace SavageExpenseTracker.Application.Dtos.Challenge
{
    public class ChallengeDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public Guid? WinnerId { get; set; }
        public Guid? LoserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status => GetStatus();

        private string GetStatus()
        {
            var now = DateTime.UtcNow;
            if (now < DateStart) return "Upcoming";
            if (now > DateEnd) return "Finished";
            return "Active";
        }
    }
}
