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
    public class UserRepositoryTests : IDisposable
    {
        private readonly SavageExpenseTrackerDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SavageExpenseTrackerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SavageExpenseTrackerDbContext(options);
            _repository = new UserRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AddAsync_ShouldAddUser_ToDatabase()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "test", PasswordHash = "hash" };

            // Act
            await _repository.AddAsync(user);

            // Assert
            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser!.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "get@example.com", UserName = "get", PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var email = "email@example.com";
            var user = new User { Id = Guid.NewGuid(), Email = email, UserName = "email", PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEmailAsync(email);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(email);
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var email = "exists@example.com";
            var user = new User { Id = Guid.NewGuid(), Email = email, UserName = "exists", PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.EmailExistsAsync(email);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser_FromDatabase()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "delete@example.com", UserName = "delete", PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(user.Id);

            // Assert
            var deletedUser = await _context.Users.FindAsync(user.Id);
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            await _context.Users.AddAsync(new User { Id = Guid.NewGuid(), Email = "1@test.com", UserName = "1", PasswordHash = "hash" });
            await _context.Users.AddAsync(new User { Id = Guid.NewGuid(), Email = "2@test.com", UserName = "2", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByUserNameAsync_ShouldReturnUser_WhenExists()
        {
            var userName = "testuser";
            var user = new User { Id = Guid.NewGuid(), Email = "u@test.com", UserName = userName, PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByUserNameAsync(userName);

            result.Should().NotBeNull();
            result!.UserName.Should().Be(userName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            var result = await _repository.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenNotFound()
        {
            var result = await _repository.GetByEmailAsync("notfound@test.com");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserNameAsync_ShouldReturnNull_WhenNotFound()
        {
            var result = await _repository.GetByUserNameAsync("notfound");
            result.Should().BeNull();
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.EmailExistsAsync("notfound@test.com");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "u@test.com", UserName = "u", PasswordHash = "hash" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            user.UserName = "new_u";
            await _repository.UpdateAsync(user);

            var updated = await _context.Users.FindAsync(user.Id);
            updated!.UserName.Should().Be("new_u");
        }
    }
}
