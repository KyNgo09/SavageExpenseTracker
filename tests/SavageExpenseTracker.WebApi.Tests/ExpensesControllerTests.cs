using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos;
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
        private readonly Guid _currentUserId;

        public ExpensesControllerTests()
        {
            _expenseServiceMock = new Mock<IExpenseService>();
            _controller = new ExpensesController(_expenseServiceMock.Object);
            _currentUserId = Guid.NewGuid();

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString()) };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetMyExpenses_ShouldReturnOkResult_WithExpenses()
        {
            // Arrange
            var pagedResult = new PagedResultDto<ExpenseDto>
            {
                Items = new List<ExpenseDto> { new ExpenseDto { Id = 1, Amount = 100 } },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };
            _expenseServiceMock.Setup(s => s.GetUserExpensesAsync(_currentUserId, 1, 10)).ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetMyExpenses(1, 10);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResult = okResult.Value.Should().BeAssignableTo<PagedResultDto<ExpenseDto>>().Subject;
            returnedResult.Items.Should().HaveCount(1);
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
            _expenseServiceMock.Setup(s => s.CreateExpenseAsync(It.Is<CreateExpenseDto>(d => d.UserId == _currentUserId)))
                .ReturnsAsync(createdExpense);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            createDto.UserId.Should().Be(_currentUserId);
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ExpensesController.GetById));
            createdResult.RouteValues?["id"].Should().Be(1);
            createdResult.Value.Should().BeEquivalentTo(createdExpense);
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenInvalidOperationExceptionThrown()
        {
            // Arrange
            var createDto = new CreateExpenseDto { Amount = 100 };
            _expenseServiceMock.Setup(s => s.CreateExpenseAsync(It.IsAny<CreateExpenseDto>()))
                .ThrowsAsync(new InvalidOperationException("Error"));

            // Act & Assert
            Func<Task> act = async () => await _controller.Create(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenUpdateFails()
        {
            // Arrange
            var updateDto = new UpdateExpenseDto { Amount = 200 };
            _expenseServiceMock.Setup(s => s.UpdateExpenseAsync(1, _currentUserId, updateDto)).ReturnsAsync(false);

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
            _expenseServiceMock.Setup(s => s.UpdateExpenseAsync(1, _currentUserId, updateDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(1, updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_ShouldThrowException_OnException()
        {
            var updateDto = new UpdateExpenseDto { Amount = 200 };
            _expenseServiceMock.Setup(s => s.UpdateExpenseAsync(1, _currentUserId, updateDto))
                .ThrowsAsync(new InvalidOperationException("Error"));
            Func<Task> act = async () => await _controller.Update(1, updateDto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
        {
            // Arrange
            _expenseServiceMock.Setup(s => s.DeleteExpenseAsync(1, _currentUserId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenDeleteSucceeds()
        {
            // Arrange
            _expenseServiceMock.Setup(s => s.DeleteExpenseAsync(1, _currentUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_ShouldThrowException_OnException()
        {
            _expenseServiceMock.Setup(s => s.DeleteExpenseAsync(1, _currentUserId))
                .ThrowsAsync(new InvalidOperationException("Error"));
            Func<Task> act = async () => await _controller.Delete(1);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }
    }
}
