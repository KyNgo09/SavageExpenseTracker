using System;

namespace SavageExpenseTracker.Domain.Entities
{
    public class ChallengeMember
    {
        public long ChallengeId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public Challenge? Challenge { get; set; }
        public User? User { get; set; }
    }
}