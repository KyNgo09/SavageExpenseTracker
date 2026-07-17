using System;
using FluentAssertions;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Domain.Tests
{
    public class ChallengeMemberTests
    {
        [Fact]
        public void ChallengeMember_ShouldInitializePropertiesCorrectly()
        {
            var userId = Guid.NewGuid();
            var joinedAt = DateTime.UtcNow;
            var challengeMember = new ChallengeMember
            {
                ChallengeId = 1,
                UserId = userId,
                JoinedAt = joinedAt
            };

            challengeMember.ChallengeId.Should().Be(1);
            challengeMember.UserId.Should().Be(userId);
            challengeMember.JoinedAt.Should().Be(joinedAt);
        }
    }
}
