using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Application.Tests
{
    public class ExpenseServiceTests
    {
        private readonly Mock<IExpenseRepository> _expenseRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly ExpenseService _expenseService;

        public ExpenseServiceTests()
        {
            _expenseRepositoryMock = new Mock<IExpenseRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _expenseService = new ExpenseService(_expenseRepositoryMock.Object, _userRepositoryMock.Object);
        }

        [Fact]
        public async Task GetUserExpensesAsync_ShouldReturnPagedResultDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expenses = new List<Expense> 
            { 
                new Expense { Id = 1, UserId = userId, Amount = 100 } 
            };
            _expenseRepositoryMock.Setup(repo => repo.GetByUserIdAsync(userId, 1, 10)).ReturnsAsync((expenses, 1));

            // Act
            var result = await _expenseService.GetUserExpensesAsync(userId, 1, 10);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Amount.Should().Be(100);
        }

        [Fact]
        public async Task GetExpenseByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Expense)null!);

            // Act
            var result = await _expenseService.GetExpenseByIdAsync(1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetExpenseByIdAsync_ShouldReturnDto_WhenFound()
        {
            var expense = new Expense { Id = 1, Amount = 100 };
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expense);

            var result = await _expenseService.GetExpenseByIdAsync(1);

            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Amount.Should().Be(100);
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var createDto = new CreateExpenseDto { UserId = Guid.NewGuid() };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(createDto.UserId)).ReturnsAsync((User)null!);

            // Act
            Func<Task> act = async () => await _expenseService.CreateExpenseAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User not found!");
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldCalculateTimeWork_WhenUserFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateExpenseDto { UserId = userId, Amount = 500 };
            var user = new User { Id = userId, HourlyRate = 100 };
            
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _expenseService.CreateExpenseAsync(createDto);

            _expenseRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Expense>(e => 
                e.TimeWork == 5 && e.AppliedHourlyRate == 100 && e.Amount == 500)), Times.Once);
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldHandleZeroHourlyRate_Correctly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateExpenseDto { UserId = userId, Amount = 500 };
            var user = new User { Id = userId, HourlyRate = 0 }; // Zero hourly rate
            
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _expenseService.CreateExpenseAsync(createDto);

            // Assert
            _expenseRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Expense>(e => 
                e.TimeWork == 0 && e.AppliedHourlyRate == 0 && e.Amount == 500)), Times.Once);
        }

        [Fact]
        public async Task UpdateExpenseAsync_ShouldReturnFalse_WhenNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UpdateExpenseDto { Amount = 100 };
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Expense)null!);

            // Act
            var result = await _expenseService.UpdateExpenseAsync(1, userId, updateDto);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateExpenseAsync_ShouldThrowException_WhenUserDoesNotOwnExpense()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var updateDto = new UpdateExpenseDto { Amount = 100 };
            var expense = new Expense { Id = 1, UserId = otherUserId };
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expense);

            // Act
            Func<Task> act = async () => await _expenseService.UpdateExpenseAsync(1, userId, updateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You do not have permission to update this expense.");
        }

        [Fact]
        public async Task UpdateExpenseAsync_ShouldCalculateTimeWork_WhenSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UpdateExpenseDto { Amount = 1000 };
            var expense = new Expense { Id = 1, UserId = userId, AppliedHourlyRate = 200, Amount = 500 };
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expense);

            // Act
            var result = await _expenseService.UpdateExpenseAsync(1, userId, updateDto);

            // Assert
            result.Should().BeTrue();
            expense.TimeWork.Should().Be(5);
            expense.Amount.Should().Be(1000);
            _expenseRepositoryMock.Verify(repo => repo.UpdateAsync(expense), Times.Once);
        }

        [Fact]
        public async Task DeleteExpenseAsync_ShouldReturnFalse_WhenNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Expense)null!);

            // Act
            var result = await _expenseService.DeleteExpenseAsync(1, userId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteExpenseAsync_ShouldThrowException_WhenUserDoesNotOwnExpense()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var expense = new Expense { Id = 1, UserId = otherUserId };
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expense);

            // Act
            Func<Task> act = async () => await _expenseService.DeleteExpenseAsync(1, userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You do not have permission to delete this expense.");
        }

        [Fact]
        public async Task DeleteExpenseAsync_ShouldDelete_WhenFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expense = new Expense { Id = 1, UserId = userId };
            _expenseRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expense);

            // Act
            var result = await _expenseService.DeleteExpenseAsync(1, userId);

            // Assert
            result.Should().BeTrue();
            _expenseRepositoryMock.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }
    }
}
