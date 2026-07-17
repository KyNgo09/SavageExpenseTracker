using System;
using System.Collections.Generic;
using FluentAssertions;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Domain.Tests
{
    public class CategoryTests
    {
        [Fact]
        public void Category_ShouldInitializePropertiesCorrectly()
        {
            var category = new Category
            {
                Id = 1,
                Name = "Food",
                CreatedAt = new DateTime(2023, 1, 1)
            };

            category.Id.Should().Be(1);
            category.Name.Should().Be("Food");
            category.CreatedAt.Should().Be(new DateTime(2023, 1, 1));
            category.Expenses.Should().NotBeNull();
            category.Expenses.Should().BeEmpty();
        }
    }
}
