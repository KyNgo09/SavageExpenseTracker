using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Options;
using SavageExpenseTracker.Infrastructure.Services;
using Xunit;

namespace SavageExpenseTracker.Infrastructure.Tests
{
    public class TokenServiceTests : IDisposable
    {
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
            {
                SecretKey = "ThisIsASecretKeyForTestingPurpose1234567890!",
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            });
            _tokenService = new TokenService(options);
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "ThisIsASecretKeyForTestingPurpose1234567890!");
            Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        }

        [Fact]
        public void GenerateAccessToken_ShouldReturnValidJwtToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                UserName = "testuser",
                Role = "User"
            };

            // Act
            var tokenString = _tokenService.GenerateAccessToken(user);

            // Assert
            tokenString.Should().NotBeNullOrWhiteSpace();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.Issuer.Should().Be("TestIssuer");
            token.Audiences.Should().Contain("TestAudience");
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value.Should().Be(user.Id.ToString());
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value.Should().Be(user.Email);
            token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value.Should().Be(user.Role);
        }

        [Fact]
        public void GenerateAccessToken_ShouldThrowException_WhenSecretKeyNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);
            var user = new User { Id = Guid.NewGuid() };

            // Act
            Action act = () => _tokenService.GenerateAccessToken(user);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JWT_SECRET_KEY environment variable is not set.");
        }

        [Fact]
        public void GenerateAccessToken_ShouldThrowException_WhenIssuerNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            var user = new User { Id = Guid.NewGuid() };

            // Act
            Action act = () => _tokenService.GenerateAccessToken(user);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JWT_ISSUER environment variable is not set.");
        }

        [Fact]
        public void GenerateAccessToken_ShouldThrowException_WhenAudienceNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
            var user = new User { Id = Guid.NewGuid() };

            // Act
            Action act = () => _tokenService.GenerateAccessToken(user);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JWT_AUDIENCE environment variable is not set.");
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnBase64String()
        {
            // Act
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Assert
            refreshToken.Should().NotBeNullOrWhiteSpace();
            // Check if it's a valid Base64 string
            Action act = () => Convert.FromBase64String(refreshToken);
            act.Should().NotThrow();
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ShouldReturnClaimsPrincipal_FromExpiredToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                UserName = "testuser",
                Role = "User"
            };
            var tokenString = _tokenService.GenerateAccessToken(user);

            // Act
            var principal = _tokenService.GetPrincipalFromExpiredToken(tokenString);

            // Assert
            principal.Should().NotBeNull();
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ShouldThrowException_WhenSecretKeyNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);

            // Act
            Action act = () => _tokenService.GetPrincipalFromExpiredToken("dummyToken");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JWT_SECRET_KEY environment variable is not set.");
        }
    }
}
