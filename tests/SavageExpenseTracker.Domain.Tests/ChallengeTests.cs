using System;
using FluentAssertions;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Domain.Tests
{
    public class ChallengeTests
    {
        [Fact]
        public void Challenge_ShouldInitializePropertiesCorrectly()
        {
            var dateStart = DateTime.UtcNow.AddDays(1);
            var dateEnd = DateTime.UtcNow.AddDays(7);
            var challenge = new Challenge
            {
                Id = 1,
                Name = "Save Money",
                DateStart = dateStart,
                DateEnd = dateEnd,
                CreatedAt = DateTime.UtcNow
            };

            challenge.Id.Should().Be(1);
            challenge.Name.Should().Be("Save Money");
            challenge.DateStart.Should().Be(dateStart);
            challenge.DateEnd.Should().Be(dateEnd);
            challenge.ChallengeMembers.Should().NotBeNull();
            challenge.ChallengeMembers.Should().BeEmpty();
        }

        [Fact]
        public void GetStatus_ShouldReturnUpcoming_WhenNowIsBeforeDateStart()
        {
            var challenge = new Challenge
            {
                DateStart = DateTime.UtcNow.AddDays(1),
                DateEnd = DateTime.UtcNow.AddDays(7)
            };
            challenge.GetStatus().Should().Be("Upcoming");
        }

        [Fact]
        public void GetStatus_ShouldReturnFinished_WhenNowIsAfterDateEnd()
        {
            var challenge = new Challenge
            {
                DateStart = DateTime.UtcNow.AddDays(-7),
                DateEnd = DateTime.UtcNow.AddDays(-1)
            };
            challenge.GetStatus().Should().Be("Finished");
        }

        [Fact]
        public void GetStatus_ShouldReturnActive_WhenNowIsBetweenDateStartAndDateEnd()
        {
            var challenge = new Challenge
            {
                DateStart = DateTime.UtcNow.AddDays(-1),
                DateEnd = DateTime.UtcNow.AddDays(1)
            };
            challenge.GetStatus().Should().Be("Active");
        }

        [Fact]
        public void GetStatus_ShouldReturnActive_WhenNowIsExactlyDateStart()
        {
            var challenge = new Challenge
            {
                DateStart = DateTime.UtcNow,
                DateEnd = DateTime.UtcNow.AddDays(1)
            };
            challenge.GetStatus().Should().Be("Active");
        }

        [Fact]
        public void GetStatus_ShouldReturnFinished_WhenNowIsExactlyDateEnd()
        {
            var challenge = new Challenge
            {
                DateStart = DateTime.UtcNow.AddDays(-1),
                DateEnd = DateTime.UtcNow
            };
            challenge.GetStatus().Should().Be("Finished");
        }
    }
}
