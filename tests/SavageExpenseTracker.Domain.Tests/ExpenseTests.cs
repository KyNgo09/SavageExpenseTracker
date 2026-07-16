using System;
using FluentAssertions;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Domain.Tests
{
    public class ExpenseTests
    {
        [Fact]
        public void Expense_ShouldInitialize_WithDefaultValues()
        {
            // Arrange & Act
            var expense = new Expense();

            // Assert
            expense.Id.Should().Be(0);
            expense.Amount.Should().Be(0);
            expense.TimeWork.Should().Be(0);
            expense.AppliedHourlyRate.Should().Be(0);
        }

        [Fact]
        public void Expense_ShouldSetProperties_Correctly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            // Act
            var expense = new Expense
            {
                Id = 1,
                UserId = userId,
                Amount = 500,
                TimeWork = 5,
                AppliedHourlyRate = 100,
                Description = "Coffee",
                CreatedAt = createdAt,
                CategoryId = 2
            };

            // Assert
            expense.Id.Should().Be(1);
            expense.UserId.Should().Be(userId);
            expense.Amount.Should().Be(500);
            expense.TimeWork.Should().Be(5);
            expense.AppliedHourlyRate.Should().Be(100);
            expense.Description.Should().Be("Coffee");
            expense.CreatedAt.Should().Be(createdAt);
            expense.CategoryId.Should().Be(2);
        }
    }
}
