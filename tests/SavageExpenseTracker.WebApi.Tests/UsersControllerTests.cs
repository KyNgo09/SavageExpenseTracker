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
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly UsersController _controller;
        private readonly Guid _currentUserId;

        public UsersControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _controller = new UsersController(
                _userServiceMock.Object, 
                _tokenServiceMock.Object, 
                _userRepositoryMock.Object);
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
            _userServiceMock.Setup(s => s.AuthenticateAsync(loginDto)).ReturnsAsync((UserDto)null!);

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
            var userDto = new UserDto { Id = Guid.NewGuid(), Email = "test@test.com" };
            var userEntity = new User { Id = userDto.Id, Email = "test@test.com" };
            
            _userServiceMock.Setup(s => s.AuthenticateAsync(loginDto)).ReturnsAsync(userDto);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userDto.Id)).ReturnsAsync(userEntity);
            
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(userEntity)).Returns("access-token-123");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token-456");

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var responseDto = okResult.Value.Should().BeAssignableTo<TokenResponseDto>().Subject;
            
            responseDto.AccessToken.Should().Be("access-token-123");
            responseDto.RefreshToken.Should().Be("refresh-token-456");
            
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.RefreshToken == "refresh-token-456")), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenUserEntityNotFound()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@test.com", Password = "password123" };
            var userDto = new UserDto { Id = Guid.NewGuid(), Email = "test@test.com" };
            
            _userServiceMock.Setup(s => s.AuthenticateAsync(loginDto)).ReturnsAsync(userDto);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userDto.Id)).ReturnsAsync((User)null!);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "User not found" });
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
            var userId = Guid.NewGuid();
            var tokenApiModel = new TokenApiModel { AccessToken = "old-access", RefreshToken = "old-refresh" };
            var user = new User 
            { 
                Id = userId, 
                RefreshToken = "old-refresh", 
                RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(1) 
            };

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken("old-access")).Returns(principal);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("new-access");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var responseDto = okResult.Value.Should().BeAssignableTo<TokenResponseDto>().Subject;
            
            responseDto.AccessToken.Should().Be("new-access");
            responseDto.RefreshToken.Should().Be("new-refresh");
            
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.RefreshToken == "new-refresh")), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenRefreshTokenExpired()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenApiModel = new TokenApiModel { AccessToken = "old-access", RefreshToken = "old-refresh" };
            var user = new User 
            { 
                Id = userId, 
                RefreshToken = "old-refresh", 
                RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(-1) // Expired
            };

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken("old-access")).Returns(principal);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Invalid client request or Refresh Token has expired" });
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenAccessTokenIsInvalid()
        {
            // Arrange
            var tokenApiModel = new TokenApiModel { AccessToken = "invalid", RefreshToken = "refresh" };
            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken("invalid")).Throws(new Exception("Invalid token"));

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Invalid access token or refresh" });
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenClaimIdIsMissingOrInvalid()
        {
            // Arrange
            var tokenApiModel = new TokenApiModel { AccessToken = "old", RefreshToken = "refresh" };
            var claims = new[] { new Claim("some_other_claim", "value") }; // No NameIdentifier
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken("old")).Returns(principal);

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Invalid Token " });
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenUserNotFoundInDb()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenApiModel = new TokenApiModel { AccessToken = "old", RefreshToken = "refresh" };
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken("old")).Returns(principal);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null!);

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Invalid client request or Refresh Token has expired" });
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnBadRequest_WhenRefreshTokenDoesNotMatch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenApiModel = new TokenApiModel { AccessToken = "old", RefreshToken = "sent-refresh" };
            var user = new User 
            { 
                Id = userId, 
                RefreshToken = "different-refresh", 
                RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(1)
            };
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken("old")).Returns(principal);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.RefreshToken(tokenApiModel);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Invalid client request or Refresh Token has expired" });
        }
    }
}
