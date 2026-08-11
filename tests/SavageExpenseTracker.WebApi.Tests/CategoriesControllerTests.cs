using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Category;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Controllers;
using Xunit;

namespace SavageExpenseTracker.WebApi.Tests
{
    public class CategoriesControllerTests
    {
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _categoryServiceMock = new Mock<ICategoryService>();
            _controller = new CategoriesController(_categoryServiceMock.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithCategories()
        {
            var pagedResult = new PagedResultDto<CategoryDto>
            {
                Items = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "Food" } },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };
            _categoryServiceMock.Setup(s => s.GetAllCategoriesAsync(1, 10)).ReturnsAsync(pagedResult);

            var result = await _controller.GetAll(1, 10);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(pagedResult);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenNull()
        {
            _categoryServiceMock.Setup(s => s.GetCategoryByIdAsync(1)).ReturnsAsync((CategoryDto)null!);
            var result = await _controller.GetById(1);
            var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { Message = "Category Not Found" });
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenExists()
        {
            var category = new CategoryDto { Id = 1, Name = "Food" };
            _categoryServiceMock.Setup(s => s.GetCategoryByIdAsync(1)).ReturnsAsync(category);

            var result = await _controller.GetById(1);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(category);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedAtAction()
        {
            var createDto = new CreateCategoryDto { Name = "Food" };
            var categoryDto = new CategoryDto { Id = 1, Name = "Food" };
            _categoryServiceMock.Setup(s => s.CreateCategoryAsync(createDto)).ReturnsAsync(categoryDto);

            var result = await _controller.Create(createDto);

            var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(CategoriesController.GetById));
            created.Value.Should().BeEquivalentTo(categoryDto);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_OnException()
        {
            var createDto = new CreateCategoryDto { Name = "Food" };
            _categoryServiceMock.Setup(s => s.CreateCategoryAsync(createDto)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.Create(createDto);

            var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenFalse()
        {
            var updateDto = new UpdateCategoryDto { Name = "Food" };
            _categoryServiceMock.Setup(s => s.UpdateCategoryAsync(1, updateDto)).ReturnsAsync(false);

            var result = await _controller.Update(1, updateDto);

            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { Message = "Category Not Found" });
        }

        [Fact]
        public async Task Update_ShouldReturnNoContent_WhenTrue()
        {
            var updateDto = new UpdateCategoryDto { Name = "Food" };
            _categoryServiceMock.Setup(s => s.UpdateCategoryAsync(1, updateDto)).ReturnsAsync(true);

            var result = await _controller.Update(1, updateDto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_OnException()
        {
            var updateDto = new UpdateCategoryDto { Name = "Food" };
            _categoryServiceMock.Setup(s => s.UpdateCategoryAsync(1, updateDto)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.Update(1, updateDto);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenFalse()
        {
            _categoryServiceMock.Setup(s => s.DeleteCategoryAsync(1)).ReturnsAsync(false);
            var result = await _controller.Delete(1);
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { Message = "Category Not Found" });
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenTrue()
        {
            _categoryServiceMock.Setup(s => s.DeleteCategoryAsync(1)).ReturnsAsync(true);
            var result = await _controller.Delete(1);
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnBadRequest_OnException()
        {
            _categoryServiceMock.Setup(s => s.DeleteCategoryAsync(1)).ThrowsAsync(new InvalidOperationException("Error"));
            var result = await _controller.Delete(1);
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }
    }
}
