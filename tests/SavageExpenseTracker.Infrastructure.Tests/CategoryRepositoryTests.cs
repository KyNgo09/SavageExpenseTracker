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
    public class CategoryRepositoryTests : IDisposable
    {
        private readonly SavageExpenseTrackerDbContext _context;
        private readonly CategoryRepository _repository;

        public CategoryRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SavageExpenseTrackerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SavageExpenseTrackerDbContext(options);
            _repository = new CategoryRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AddAsync_ShouldAddCategory_ToDatabase()
        {
            var category = new Category { Name = "Food" };
            await _repository.AddAsync(category);

            var savedCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Food");
            savedCategory.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCategories()
        {
            await _context.Categories.AddAsync(new Category { Name = "Cat 1" });
            await _context.Categories.AddAsync(new Category { Name = "Cat 2" });
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCategory_WhenExists()
        {
            var category = new Category { Name = "Food" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(category.Id);
            result.Should().NotBeNull();
            result!.Name.Should().Be("Food");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCategory()
        {
            var category = new Category { Name = "Food" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            category.Name = "Updated Food";
            await _repository.UpdateAsync(category);

            var updatedCategory = await _context.Categories.FindAsync(category.Id);
            updatedCategory!.Name.Should().Be("Updated Food");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveCategory()
        {
            var category = new Category { Name = "Food" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(category.Id);

            var deleted = await _context.Categories.FindAsync(category.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task HasExpensesAsync_ShouldReturnTrue_WhenCategoryHasExpenses()
        {
            var category = new Category { Name = "Food" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            await _context.Expenses.AddAsync(new Expense { CategoryId = category.Id, Amount = 10 });
            await _context.SaveChangesAsync();

            var result = await _repository.HasExpensesAsync(category.Id);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasExpensesAsync_ShouldReturnFalse_WhenNoExpenses()
        {
            var category = new Category { Name = "Food" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            var result = await _repository.HasExpensesAsync(category.Id);
            result.Should().BeFalse();
        }
    }
}
