using System;
using System.Collections.Generic;
using SavageExpenseTracker.Domain.Constants;

namespace SavageExpenseTracker.Domain.Entities
{
    public class Challenge
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public Guid? WinnerId { get; set; }
        public Guid? LoserId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? Winner { get; set; }
        public User? Loser { get; set; }
        public ICollection<ChallengeMember> ChallengeMembers { get; set; } = new List<ChallengeMember>();

        public string GetStatus()
        {
            var now = DateTime.UtcNow;
            if (now < DateStart) return ChallengeStatus.Upcoming;
            if (now > DateEnd) return ChallengeStatus.Finished;
            return ChallengeStatus.Active;
        }
    }
}