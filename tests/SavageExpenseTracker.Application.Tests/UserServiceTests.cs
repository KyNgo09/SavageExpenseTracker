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
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _userService = new UserService(_userRepositoryMock.Object);
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

            // Act
            var result = await _userService.RegisterUserAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(createDto.Email);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.Is<User>(u => 
                u.Email == createDto.Email && u.UserName == createDto.UserName && u.PasswordHash != null)), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync((User)null!);

            // Act
            var result = await _userService.AuthenticateAsync(loginDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIncorrect()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };
            var user = new User { Email = "test@example.com", PasswordHash = PasswordHasher.HashPassword("correctpassword") };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);

            // Act
            var result = await _userService.AuthenticateAsync(loginDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnUser_WhenSuccess()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
            var user = new User { Email = "test@example.com", PasswordHash = PasswordHasher.HashPassword("password123") };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);

            // Act
            var result = await _userService.AuthenticateAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowException_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var dto = new ChangePasswordDto { NewPassword = "new", ConfirmNewPassword = "different" };

            // Act
            Func<Task> act = async () => await _userService.ChangePasswordAsync(Guid.NewGuid(), dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("New password and confirm new password do not match!");
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowException_WhenOldPasswordIncorrect()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new ChangePasswordDto { OldPassword = "wrong", NewPassword = "new", ConfirmNewPassword = "new" };
            var user = new User { Id = userId, PasswordHash = PasswordHasher.HashPassword("correct") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await _userService.ChangePasswordAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Incorrect old password!");
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowException_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new ChangePasswordDto { OldPassword = "old", NewPassword = "new", ConfirmNewPassword = "new" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null!);

            Func<Task> act = async () => await _userService.ChangePasswordAsync(userId, dto);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User not found!");
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldUpdatePassword_WhenSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new ChangePasswordDto { OldPassword = "old", NewPassword = "new", ConfirmNewPassword = "new" };
            var user = new User { Id = userId, PasswordHash = PasswordHasher.HashPassword("old") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, dto);

            // Assert
            result.Should().BeTrue();
            user.PasswordHash.Should().Be(PasswordHasher.HashPassword("new"));
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task GetAllUserAsync_ShouldReturnDtoList()
        {
            var users = new List<User> { new User { Id = Guid.NewGuid(), Email = "a@test.com" } };
            _userRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(users);

            var result = await _userService.GetAllUserAsync();

            result.Should().HaveCount(1);
            result.First().Email.Should().Be("a@test.com");
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
        public async Task SearchUserByEmailAsync_ShouldReturnMatchingUsers()
        {
            var users = new List<User> 
            { 
                new User { Email = "test@example.com" },
                new User { Email = "other@example.com" },
                new User { Email = "test2@test.com" }
            };
            _userRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(users);

            var result = (await _userService.SearchUserByEmailAsync("test")).ToList();

            result.Should().HaveCount(2);
            result[0].Email.Should().Be("test@example.com");
            result[1].Email.Should().Be("test2@test.com");
        }
    }
}
