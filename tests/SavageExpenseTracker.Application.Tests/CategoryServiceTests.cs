using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SavageExpenseTracker.Application.Dtos.Category;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Application.Tests
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _service = new CategoryService(_categoryRepoMock.Object);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnDtoList()
        {
            var categories = new List<Category> { new Category { Id = 1, Name = "Food" } };
            _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

            var result = await _service.GetAllCategoriesAsync();

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Food");
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnDto_WhenExists()
        {
            var category = new Category { Id = 1, Name = "Food" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

            var result = await _service.GetCategoryByIdAsync(1);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Food");
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Category)null!);
            var result = await _service.GetCategoryByIdAsync(1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldAddCategoryAndReturnDto()
        {
            var createDto = new CreateCategoryDto { Name = "Food" };

            var result = await _service.CreateCategoryAsync(createDto);

            result.Should().NotBeNull();
            result.Name.Should().Be("Food");
            _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Category)null!);

            var result = await _service.UpdateCategoryAsync(1, new UpdateCategoryDto { Name = "New" });

            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldUpdateAndReturnTrue_WhenCategoryExists()
        {
            var category = new Category { Id = 1, Name = "Old" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

            var result = await _service.UpdateCategoryAsync(1, new UpdateCategoryDto { Name = "New" });

            result.Should().BeTrue();
            category.Name.Should().Be("New");
            _categoryRepoMock.Verify(r => r.UpdateAsync(category), Times.Once);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Category)null!);

            var result = await _service.DeleteCategoryAsync(1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldThrowException_WhenCategoryHasExpenses()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1 });
            _categoryRepoMock.Setup(r => r.HasExpensesAsync(1)).ReturnsAsync(true);

            Func<Task> act = async () => await _service.DeleteCategoryAsync(1);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Category has expenses and cannot be deleted.");
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldDeleteAndReturnTrue_WhenCategoryHasNoExpenses()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1 });
            _categoryRepoMock.Setup(r => r.HasExpensesAsync(1)).ReturnsAsync(false);

            var result = await _service.DeleteCategoryAsync(1);

            result.Should().BeTrue();
            _categoryRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
