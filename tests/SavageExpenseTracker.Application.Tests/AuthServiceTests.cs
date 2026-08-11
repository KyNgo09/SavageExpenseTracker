using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Application.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _authService = new AuthService(_userRepositoryMock.Object, _tokenServiceMock.Object, _passwordHasherMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync((User)null!);

            // Act
            var result = await _authService.AuthenticateAsync(loginDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIncorrect()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };
            var user = new User { Email = "test@example.com", PasswordHash = "hashed_correctpassword" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword("wrongpassword", "hashed_correctpassword")).Returns(false);

            // Act
            var result = await _authService.AuthenticateAsync(loginDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnUser_WhenSuccess()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
            var user = new User { Email = "test@example.com", PasswordHash = "hashed_password123" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword("password123", "hashed_password123")).Returns(true);

            // Act
            var result = await _authService.AuthenticateAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnTokenResponse_WhenSuccess()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed_password123" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword("password123", "hashed_password123")).Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("mock_access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("mock_refresh_token");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result!.AccessToken.Should().Be("mock_access_token");
            result.RefreshToken.Should().Be("mock_refresh_token");
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<User>(u => u.RefreshToken == "mock_refresh_token")), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowException_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var dto = new ChangePasswordDto { NewPassword = "new", ConfirmNewPassword = "different" };

            // Act
            Func<Task> act = async () => await _authService.ChangePasswordAsync(Guid.NewGuid(), dto);

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
            var user = new User { Id = userId, PasswordHash = "hashed_correct" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword("wrong", "hashed_correct")).Returns(false);

            // Act
            Func<Task> act = async () => await _authService.ChangePasswordAsync(userId, dto);

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

            Func<Task> act = async () => await _authService.ChangePasswordAsync(userId, dto);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User not found!");
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldUpdatePassword_WhenSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new ChangePasswordDto { OldPassword = "old", NewPassword = "new", ConfirmNewPassword = "new" };
            var user = new User { Id = userId, PasswordHash = "hashed_old" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword("old", "hashed_old")).Returns(true);
            _passwordHasherMock.Setup(p => p.HashPassword("new")).Returns("hashed_new");

            // Act
            var result = await _authService.ChangePasswordAsync(userId, dto);

            // Assert
            result.Should().BeTrue();
            user.PasswordHash.Should().Be("hashed_new");
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(user), Times.Once);
        }
    }
}
