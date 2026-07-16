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
    }
}
