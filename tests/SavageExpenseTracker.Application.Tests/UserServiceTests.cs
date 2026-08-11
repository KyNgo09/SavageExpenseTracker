using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Application.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPhotoService> _photoServiceMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _photoServiceMock = new Mock<IPhotoService>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _userService = new UserService(_userRepositoryMock.Object, _photoServiceMock.Object, _passwordHasherMock.Object);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrowException_WhenEmailExists()
        {
            // Arrange
            var createDto = new CreateUserDto { Email = "test@example.com" };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(createDto.Email)).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _userService.RegisterUserAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email already exists!");
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrowException_WhenUserNameExists()
        {
            // Arrange
            var createDto = new CreateUserDto { Email = "new@example.com", UserName = "existinguser" };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(createDto.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(repo => repo.UserNameExistsAsync(createDto.UserName)).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _userService.RegisterUserAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Username already exists!");
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldAddUser_WhenSuccess()
        {
            // Arrange
            var createDto = new CreateUserDto { Email = "test@example.com", Password = "password123", UserName = "testuser" };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(createDto.Email)).ReturnsAsync(false);
            _passwordHasherMock.Setup(p => p.HashPassword(createDto.Password)).Returns("hashed_password123");

            // Act
            var result = await _userService.RegisterUserAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(createDto.Email);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.Is<User>(u => 
                u.Email == createDto.Email && u.UserName == createDto.UserName && u.PasswordHash == "hashed_password123")), Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnPagedResultDto()
        {
            var users = new List<User> { new User { Id = Guid.NewGuid(), Email = "a@test.com" } };
            _userRepositoryMock.Setup(repo => repo.GetAllAsync(1, 10)).ReturnsAsync((users, 1));

            var result = await _userService.GetAllUsersAsync(1, 10);

            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Email.Should().Be("a@test.com");
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnDto_WhenFound()
        {
            var userId = Guid.NewGuid();
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, Email = "test@test.com" });

            var result = await _userService.GetUserByIdAsync(userId);

            result.Should().NotBeNull();
            result!.Email.Should().Be("test@test.com");
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);
            var result = await _userService.GetUserByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldReturnDto_WhenFound()
        {
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync("test@test.com")).ReturnsAsync(new User { Email = "test@test.com" });
            var result = await _userService.GetUserByEmailAsync("test@test.com");
            result.Should().NotBeNull();
            result!.Email.Should().Be("test@test.com");
        }

        [Fact]
        public async Task GetUserByUserNameAsync_ShouldReturnDto_WhenFound()
        {
            _userRepositoryMock.Setup(repo => repo.GetByUserNameAsync("username")).ReturnsAsync(new User { UserName = "username" });
            var result = await _userService.GetUserByUserNameAsync("username");
            result.Should().NotBeNull();
            result!.UserName.Should().Be("username");
        }

        [Fact]
        public async Task GetUserByUserNameAsync_ShouldReturnNull_WhenNotFound()
        {
            _userRepositoryMock.Setup(repo => repo.GetByUserNameAsync("username")).ReturnsAsync((User)null!);
            var result = await _userService.GetUserByUserNameAsync("username");
            result.Should().BeNull();
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnBoolean()
        {
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync("test@test.com")).ReturnsAsync(true);
            var result = await _userService.EmailExistsAsync("test@test.com");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnFalse_WhenNotFound()
        {
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);
            var result = await _userService.UpdateUserAsync(Guid.NewGuid(), new UpdateUserDto());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldThrowException_WhenUserNameExists()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "oldname" };
            var updateDto = new UpdateUserDto { UserName = "takenname" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(repo => repo.UserNameExistsAsync("takenname")).ReturnsAsync(true);

            Func<Task> act = async () => await _userService.UpdateUserAsync(userId, updateDto);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Username already exists!");
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdateAndReturnTrue_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "old" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _userService.UpdateUserAsync(userId, new UpdateUserDto { UserName = "new", HourlyRate = 100 });

            result.Should().BeTrue();
            user.UserName.Should().Be("new");
            user.HourlyRate.Should().Be(100);
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenNotFound()
        {
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);
            var result = await _userService.DeleteUserAsync(Guid.NewGuid());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldDeleteAndReturnTrue_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId });

            var result = await _userService.DeleteUserAsync(userId);

            result.Should().BeTrue();
            _userRepositoryMock.Verify(repo => repo.DeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task SearchUserByEmailAsync_ShouldReturnMatchingPagedUsers()
        {
            var users = new List<User> 
            { 
                new User { Email = "test@example.com" },
                new User { Email = "test2@test.com" }
            };
            _userRepositoryMock.Setup(repo => repo.SearchByEmailAsync("test", 1, 10)).ReturnsAsync((users, 2));

            var result = await _userService.SearchUserByEmailAsync("test", 1, 10);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.First().Email.Should().Be("test@example.com");
        }
    }
}
