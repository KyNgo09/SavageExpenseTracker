using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos.Friendship;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Controllers;
using Xunit;

namespace SavageExpenseTracker.WebApi.Tests
{
    public class FriendshipControllerTests
    {
        private readonly Mock<IFriendshipService> _friendshipServiceMock;
        private readonly FriendshipController _controller;

        public FriendshipControllerTests()
        {
            _friendshipServiceMock = new Mock<IFriendshipService>();
            _controller = new FriendshipController(_friendshipServiceMock.Object);
        }

        [Fact]
        public async Task GetFriends_ShouldReturnOk_WithFriendsList()
        {
            // Arrange
            var friends = new List<UserProfileDto> { new UserProfileDto { Id = Guid.NewGuid(), UserName = "friend1" } };
            _friendshipServiceMock.Setup(s => s.GetFriendsListAsync(It.IsAny<Guid>())).ReturnsAsync(friends);

            // Act
            var result = await _controller.GetFriends();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(friends);
        }

        [Fact]
        public async Task SendRequest_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.SendRequestAsync(It.IsAny<Guid>(), targetUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.SendRequest(targetUserId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Đã gửi lời mời kết bạn." });
        }

        [Fact]
        public async Task SendRequest_ShouldReturnBadRequest_WhenInvalidOperationExceptionThrown()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.SendRequestAsync(It.IsAny<Guid>(), targetUserId))
                .ThrowsAsync(new InvalidOperationException("Error"));

            // Act
            var result = await _controller.SendRequest(targetUserId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task AcceptRequest_ShouldReturnNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            _friendshipServiceMock.Setup(s => s.AcceptRequestAsync(It.IsAny<Guid>(), 1)).ReturnsAsync(false);

            // Act
            var result = await _controller.AcceptRequest(1);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task AcceptRequest_ShouldReturnOk_WhenServiceReturnsTrue()
        {
            // Arrange
            _friendshipServiceMock.Setup(s => s.AcceptRequestAsync(It.IsAny<Guid>(), 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.AcceptRequest(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Đã chấp nhận kết bạn." });
        }

        [Fact]
        public async Task Unfriend_ShouldReturnNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            var friendId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.UnfriendAsync(It.IsAny<Guid>(), friendId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Unfriend(friendId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Unfriend_ShouldReturnOk_WhenServiceReturnsTrue()
        {
            // Arrange
            var friendId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.UnfriendAsync(It.IsAny<Guid>(), friendId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Unfriend(friendId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Đã hủy kết bạn." });
        }
    }
}
