using System;
using SavageExpenseTracker.Domain.Constants;

namespace SavageExpenseTracker.Domain.Entities
{
    public class Friendship
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }  // The person performing the action (sending an invitation, or blocking)
        public Guid FriendId { get; set; } // The person receiving the action
        public string Status { get; set; } = FriendshipStatus.Pending;
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public User? Friend { get; set; }
    }
}