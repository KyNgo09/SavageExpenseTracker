using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.WebApi.Controllers;
using Xunit;

namespace SavageExpenseTracker.WebApi.Tests
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly UsersController _controller;
        private readonly Guid _currentUserId;

        public UsersControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _authServiceMock = new Mock<IAuthService>();
            _controller = new UsersController(_userServiceMock.Object, _authServiceMock.Object);
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
        public async Task GetMe_ShouldReturnOk_WhenUserExists()
        {
            // Arrange
            var userDto = new UserDto { Id = _currentUserId, Email = "me@test.com" };
            _userServiceMock.Setup(s => s.GetUserByIdAsync(_currentUserId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetMe();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(userDto);
        }

        [Fact]
        public async Task GetMe_ShouldReturnNotFound_WhenUserIsNull()
        {
            // Arrange
            _userServiceMock.Setup(s => s.GetUserByIdAsync(_currentUserId)).ReturnsAsync((UserDto)null!);

            // Act
            var result = await _controller.GetMe();

            // Assert
            var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { Message = "User not found" });
        }

        [Fact]
        public async Task UpdateMe_ShouldReturnNoContent_WhenUpdateSucceeds()
        {
            // Arrange
            var updateDto = new UpdateUserDto { UserName = "UpdatedName" };
            _userServiceMock.Setup(s => s.UpdateUserAsync(_currentUserId, updateDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateMe(updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_ShouldReturnForbid_WhenUserIsNotSelfAndNotAdmin()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var updateDto = new UpdateUserDto { UserName = "Malicious" };

            // Act
            var result = await _controller.Update(otherUserId, updateDto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ChangePassword_ShouldReturnForbid_WhenUserIsNotSelf()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var changePasswordDto = new ChangePasswordDto { OldPassword = "1", NewPassword = "2", ConfirmNewPassword = "2" };

            // Act
            var result = await _controller.ChangePassword(otherUserId, changePasswordDto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@test.com", Password = "wrongpassword" };
            _authServiceMock.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync((TokenResponseDto)null!);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = "Invalid Email or Password" });
        }

        [Fact]
        public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@test.com", Password = "password123" };
            var tokenResponse = new TokenResponseDto { AccessToken = "access-token-123", RefreshToken = "refresh-token-456" };
            
            _authServiceMock.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync(tokenResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var responseDto = okResult.Value.Should().BeAssignableTo<TokenResponseDto>().Subject;
            
            responseDto.AccessToken.Should().Be("access-token-123");
            responseDto.RefreshToken.Should().Be("refresh-token-456");
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            // Act
            var result = await _controller.RefreshToken(null!);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Invalid client request");
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnNewTokens_WhenValid()
        {
            // Arrange
            var tokenApiModel = new TokenApiModel { AccessToken = "old-access", RefreshToken = "old-refresh" };
            var tokenResponse = new TokenResponseDto { AccessToken = "new-access", RefreshToken = "new-refresh" };

            _authServiceMock.Setup(s => s.RefreshTokenAsync(tokenApiModel)).ReturnsAsync(tokenResponse);

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var responseDto = okResult.Value.Should().BeAssignableTo<TokenResponseDto>().Subject;
            
            responseDto.AccessToken.Should().Be("new-access");
            responseDto.RefreshToken.Should().Be("new-refresh");
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenRefreshTokenExpired()
        {
            // Arrange
            var tokenApiModel = new TokenApiModel { AccessToken = "old-access", RefreshToken = "old-refresh" };
            _authServiceMock.Setup(s => s.RefreshTokenAsync(tokenApiModel)).ReturnsAsync((TokenResponseDto)null!);

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Invalid client request or Refresh Token has expired" });
        }
    }
}
