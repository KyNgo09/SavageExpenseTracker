using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Friendship;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Controllers;
using Xunit;

namespace SavageExpenseTracker.WebApi.Tests
{
    public class FriendshipControllerTests
    {
        private readonly Mock<IFriendshipService> _friendshipServiceMock;
        private readonly FriendshipsController _controller;
        private readonly Guid _currentUserId;

        public FriendshipControllerTests()
        {
            _friendshipServiceMock = new Mock<IFriendshipService>();
            _controller = new FriendshipsController(_friendshipServiceMock.Object);
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
        public async Task GetFriends_ShouldReturnOk_WithFriendsList()
        {
            // Arrange
            var pagedResult = new PagedResultDto<UserProfileDto>
            {
                Items = new List<UserProfileDto> { new UserProfileDto { Id = Guid.NewGuid(), UserName = "friend1" } },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };
            _friendshipServiceMock.Setup(s => s.GetFriendsListAsync(_currentUserId, 1, 10)).ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetFriends(1, 10);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(pagedResult);
        }

        [Fact]
        public async Task SendRequest_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.SendRequestAsync(_currentUserId, targetUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.SendRequest(targetUserId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Friend request sent." });
        }

        [Fact]
        public async Task SendRequest_ShouldThrowException_WhenInvalidOperationExceptionThrown()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.SendRequestAsync(_currentUserId, targetUserId))
                .ThrowsAsync(new InvalidOperationException("Error"));

            // Act & Assert
            Func<Task> act = async () => await _controller.SendRequest(targetUserId);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }

        [Fact]
        public async Task AcceptRequest_ShouldReturnNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            _friendshipServiceMock.Setup(s => s.AcceptRequestAsync(_currentUserId, 1)).ReturnsAsync(false);

            // Act
            var result = await _controller.AcceptRequest(1);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task AcceptRequest_ShouldReturnOk_WhenServiceReturnsTrue()
        {
            // Arrange
            _friendshipServiceMock.Setup(s => s.AcceptRequestAsync(_currentUserId, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.AcceptRequest(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Friend request accepted." });
        }

        [Fact]
        public async Task Unfriend_ShouldReturnNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            var friendId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.UnfriendAsync(_currentUserId, friendId)).ReturnsAsync(false);

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
            _friendshipServiceMock.Setup(s => s.UnfriendAsync(_currentUserId, friendId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Unfriend(friendId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Friendship removed." });
        }

        [Fact]
        public async Task GetPendingRequests_ShouldReturnOk_WithRequestsList()
        {
            var pagedResult = new PagedResultDto<FriendshipRequestDto>
            {
                Items = new List<FriendshipRequestDto> { new FriendshipRequestDto { FriendshipId = 1, SenderName = "sender" } },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };
            _friendshipServiceMock.Setup(s => s.GetPendingRequestsAsync(_currentUserId, 1, 10)).ReturnsAsync(pagedResult);

            var result = await _controller.GetPendingRequests(1, 10);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(pagedResult);
        }

        [Fact]
        public async Task RejectRequest_ShouldReturnOk_WhenSuccess()
        {
            _friendshipServiceMock.Setup(s => s.RejectRequestAsync(_currentUserId, 1)).ReturnsAsync(true);
            var result = await _controller.RejectRequest(1);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Friend request rejected." });
        }

        [Fact]
        public async Task RejectRequest_ShouldReturnNotFound_WhenReturnsFalse()
        {
            _friendshipServiceMock.Setup(s => s.RejectRequestAsync(_currentUserId, 1)).ReturnsAsync(false);
            var result = await _controller.RejectRequest(1);
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task RejectRequest_ShouldThrowException_OnException()
        {
            _friendshipServiceMock.Setup(s => s.RejectRequestAsync(_currentUserId, 1)).ThrowsAsync(new InvalidOperationException("Error"));
            Func<Task> act = async () => await _controller.RejectRequest(1);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }

        [Fact]
        public async Task AcceptRequest_ShouldThrowException_OnException()
        {
            _friendshipServiceMock.Setup(s => s.AcceptRequestAsync(_currentUserId, 1)).ThrowsAsync(new InvalidOperationException("Error"));
            Func<Task> act = async () => await _controller.AcceptRequest(1);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }

        [Fact]
        public async Task BlockUser_ShouldReturnOk_WhenSuccess()
        {
            var targetId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.BlockUserAsync(_currentUserId, targetId)).ReturnsAsync(true);
            var result = await _controller.BlockUser(targetId);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "User blocked." });
        }

        [Fact]
        public async Task BlockUser_ShouldThrowException_OnException()
        {
            var targetId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.BlockUserAsync(_currentUserId, targetId)).ThrowsAsync(new InvalidOperationException("Error"));
            Func<Task> act = async () => await _controller.BlockUser(targetId);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error");
        }

        [Fact]
        public async Task UnblockUser_ShouldReturnOk_WhenSuccess()
        {
            var targetId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.UnblockUserAsync(_currentUserId, targetId)).ReturnsAsync(true);
            var result = await _controller.UnblockUser(targetId);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "User unblocked." });
        }

        [Fact]
        public async Task UnblockUser_ShouldReturnNotFound_WhenReturnsFalse()
        {
            var targetId = Guid.NewGuid();
            _friendshipServiceMock.Setup(s => s.UnblockUserAsync(_currentUserId, targetId)).ReturnsAsync(false);
            var result = await _controller.UnblockUser(targetId);
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
