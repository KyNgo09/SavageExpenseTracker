using System;
using FluentAssertions;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Domain.Tests
{
    public class UserTests
    {
        [Fact]
        public void User_ShouldInitialize_WithDefaultValues()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            user.Id.Should().Be(Guid.Empty);
            user.HourlyRate.Should().Be(0);
        }

        [Fact]
        public void User_ShouldSetProperties_Correctly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            // Act
            var user = new User
            {
                Id = id,
                UserName = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                HourlyRate = 200,
                CreatedAt = createdAt
            };

            // Assert
            user.Id.Should().Be(id);
            user.UserName.Should().Be("testuser");
            user.Email.Should().Be("test@example.com");
            user.PasswordHash.Should().Be("hashedpassword");
            user.HourlyRate.Should().Be(200);
            user.CreatedAt.Should().Be(createdAt);
        }
    }
}
