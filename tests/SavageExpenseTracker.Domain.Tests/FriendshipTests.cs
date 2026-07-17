using System;
using FluentAssertions;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Domain.Tests
{
    public class FriendshipTests
    {
        [Fact]
        public void Friendship_ShouldInitializePropertiesCorrectly()
        {
            var userId = Guid.NewGuid();
            var friendId = Guid.NewGuid();
            var friendship = new Friendship
            {
                Id = 1,
                UserId = userId,
                FriendId = friendId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            friendship.Id.Should().Be(1);
            friendship.UserId.Should().Be(userId);
            friendship.FriendId.Should().Be(friendId);
            friendship.Status.Should().Be("pending");
        }
    }
}
