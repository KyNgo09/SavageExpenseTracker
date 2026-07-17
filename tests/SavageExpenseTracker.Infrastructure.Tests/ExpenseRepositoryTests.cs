using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;
using SavageExpenseTracker.Infrastructure.Repositories;
using Xunit;

namespace SavageExpenseTracker.Infrastructure.Tests
{
    public class ExpenseRepositoryTests : IDisposable
    {
        private readonly SavageExpenseTrackerDbContext _context;
        private readonly ExpenseRepository _repository;

        public ExpenseRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SavageExpenseTrackerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SavageExpenseTrackerDbContext(options);
            _repository = new ExpenseRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AddAsync_ShouldAddExpense_ToDatabase()
        {
            // Arrange
            var expense = new Expense { Id = 1, UserId = Guid.NewGuid(), Amount = 100, CreatedAt = DateTime.UtcNow };

            // Act
            await _repository.AddAsync(expense);

            // Assert
            var savedExpense = await _context.Expenses.FindAsync(expense.Id);
            savedExpense.Should().NotBeNull();
            savedExpense!.Amount.Should().Be(100);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnExpense_WhenExists()
        {
            // Arrange
            var expense = new Expense { Id = 2, UserId = Guid.NewGuid(), Amount = 200, CreatedAt = DateTime.UtcNow };
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(expense.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(expense.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnExpenses_OrderedByDescendingCreatedAt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense1 = new Expense { Id = 3, UserId = userId, Amount = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
            var expense2 = new Expense { Id = 4, UserId = userId, Amount = 200, CreatedAt = DateTime.UtcNow };
            var expense3 = new Expense { Id = 5, UserId = Guid.NewGuid(), Amount = 300, CreatedAt = DateTime.UtcNow }; // Diff user

            await _context.Expenses.AddRangeAsync(expense1, expense2, expense3);
            await _context.SaveChangesAsync();

            // Act
            var results = await _repository.GetByUserIdAsync(userId);
            var resultsList = results.ToList();

            // Assert
            resultsList.Should().HaveCount(2);
            resultsList[0].Id.Should().Be(4); // Newest first
            resultsList[1].Id.Should().Be(3);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveExpense_FromDatabase()
        {
            // Arrange
            var expense = new Expense { Id = 6, UserId = Guid.NewGuid(), Amount = 100, CreatedAt = DateTime.UtcNow };
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(expense.Id);

            // Assert
            var deletedExpense = await _context.Expenses.FindAsync(expense.Id);
            deletedExpense.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExpense_InDatabase()
        {
            // Arrange
            var expense = new Expense { Id = 7, UserId = Guid.NewGuid(), Amount = 100, CreatedAt = DateTime.UtcNow };
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();

            // Act
            expense.Amount = 500;
            await _repository.UpdateAsync(expense);

            // Assert
            var updatedExpense = await _context.Expenses.FindAsync(expense.Id);
            updatedExpense.Should().NotBeNull();
            updatedExpense!.Amount.Should().Be(500);
        }
    }
}
