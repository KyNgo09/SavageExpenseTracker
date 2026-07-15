using System;

namespace SavageExpenseTracker.Application.Dtos.Friendship
{
    public class FriendshipRequestDto
    {
        public long FriendshipId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}