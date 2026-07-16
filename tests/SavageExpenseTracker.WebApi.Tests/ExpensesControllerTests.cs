using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Controllers;
using Xunit;

namespace SavageExpenseTracker.WebApi.Tests
{
    public class ExpensesControllerTests
    {
        private readonly Mock<IExpenseService> _expenseServiceMock;
        private readonly ExpensesController _controller;

        public ExpensesControllerTests()
        {
            _expenseServiceMock = new Mock<IExpenseService>();
            _controller = new ExpensesController(_expenseServiceMock.Object);
        }

        [Fact]
        public async Task GetUserExpenses_ShouldReturnOkResult_WithExpenses()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expenses = new List<ExpenseDto> { new ExpenseDto { Id = 1, Amount = 100 } };
            _expenseServiceMock.Setup(s => s.GetUserExpensesAsync(userId)).ReturnsAsync(expenses);

            // Act
            var result = await _controller.GetUserExpenses(userId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedExpenses = okResult.Value.Should().BeAssignableTo<IEnumerable<ExpenseDto>>().Subject;
            returnedExpenses.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenExpenseIsNull()
        {
            // Arrange
            _expenseServiceMock.Setup(s => s.GetExpenseByIdAsync(1)).ReturnsAsync((ExpenseDto)null!);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenExpenseExists()
        {
            // Arrange
            var expense = new ExpenseDto { Id = 1, Amount = 100 };
            _expenseServiceMock.Setup(s => s.GetExpenseByIdAsync(1)).ReturnsAsync(expense);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expense);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedAtAction_WhenSuccess()
        {
            // Arrange
            var createDto = new CreateExpenseDto { Amount = 100 };
            var createdExpense = new ExpenseDto { Id = 1, Amount = 100 };
            _expenseServiceMock.Setup(s => s.CreateExpenseAsync(createDto)).ReturnsAsync(createdExpense);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ExpensesController.GetById));
            createdResult.RouteValues?["id"].Should().Be(1);
            createdResult.Value.Should().BeEquivalentTo(createdExpense);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenInvalidOperationExceptionThrown()
        {
            // Arrange
            var createDto = new CreateExpenseDto { Amount = 100 };
            _expenseServiceMock.Setup(s => s.CreateExpenseAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("Error"));

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenUpdateFails()
        {
            // Arrange
            var updateDto = new UpdateExpenseDto { Amount = 200 };
            _expenseServiceMock.Setup(s => s.UpdateExpenseAsync(1, updateDto)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(1, updateDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Update_ShouldReturnNoContent_WhenUpdateSucceeds()
        {
            // Arrange
            var updateDto = new UpdateExpenseDto { Amount = 200 };
            _expenseServiceMock.Setup(s => s.UpdateExpenseAsync(1, updateDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(1, updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
        {
            // Arrange
            _expenseServiceMock.Setup(s => s.DeleteExpenseAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenDeleteSucceeds()
        {
            // Arrange
            _expenseServiceMock.Setup(s => s.DeleteExpenseAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }
    }
}
