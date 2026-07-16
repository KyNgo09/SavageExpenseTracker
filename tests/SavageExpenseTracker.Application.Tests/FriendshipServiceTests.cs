using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Application.Tests
{
    public class FriendshipServiceTests
    {
        private readonly Mock<IFriendshipRepository> _friendshipRepositoryMock;
        private readonly FriendshipService _friendshipService;

        public FriendshipServiceTests()
        {
            _friendshipRepositoryMock = new Mock<IFriendshipRepository>();
            _friendshipService = new FriendshipService(_friendshipRepositoryMock.Object);
        }

        [Fact]
        public async Task SendRequestAsync_ShouldThrowException_WhenSendingToSelf()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _friendshipService.SendRequestAsync(userId, userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot send a friend request to yourself.");
        }

        [Fact]
        public async Task SendRequestAsync_ShouldThrowException_WhenAlreadyPending()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(new Friendship { Status = "pending" });

            // Act
            Func<Task> act = async () => await _friendshipService.SendRequestAsync(currentUserId, targetUserId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Friend request is already pending.");
        }

        [Fact]
        public async Task SendRequestAsync_ShouldAddFriendship_WhenSuccess()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync((Friendship)null!);

            // Act
            var result = await _friendshipService.SendRequestAsync(currentUserId, targetUserId);

            // Assert
            result.Should().BeTrue();
            _friendshipRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Friendship>(f => 
                f.UserId == currentUserId && 
                f.FriendId == targetUserId && 
                f.Status == "pending")), Times.Once);
        }

        [Fact]
        public async Task AcceptRequestAsync_ShouldReturnFalse_WhenFriendshipNotFound()
        {
            // Arrange
            _friendshipRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((Friendship)null!);

            // Act
            var result = await _friendshipService.AcceptRequestAsync(Guid.NewGuid(), 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AcceptRequestAsync_ShouldThrowException_WhenWrongUserOrStatus()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var friendship = new Friendship { FriendId = Guid.NewGuid(), Status = "pending" };
            _friendshipRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync(friendship);

            // Act
            Func<Task> act = async () => await _friendshipService.AcceptRequestAsync(currentUserId, 1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You do not have permission to accept this request.");
        }

        [Fact]
        public async Task AcceptRequestAsync_ShouldUpdateStatusToAccepted_WhenSuccess()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var friendship = new Friendship { FriendId = currentUserId, Status = "pending" };
            _friendshipRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync(friendship);

            // Act
            var result = await _friendshipService.AcceptRequestAsync(currentUserId, 1);

            // Assert
            result.Should().BeTrue();
            friendship.Status.Should().Be("accepted");
            _friendshipRepositoryMock.Verify(repo => repo.UpdateAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldThrowException_WhenBlockingSelf()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _friendshipService.BlockUserAsync(userId, userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot block yourself.");
        }

        [Fact]
        public async Task BlockUserAsync_ShouldUpdateExistingFriendship_WhenFriendshipExists()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var existingFriendship = new Friendship { Status = "accepted" };
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(existingFriendship);

            // Act
            var result = await _friendshipService.BlockUserAsync(currentUserId, targetUserId);

            // Assert
            result.Should().BeTrue();
            existingFriendship.Status.Should().Be("blocked");
            existingFriendship.UserId.Should().Be(currentUserId);
            existingFriendship.FriendId.Should().Be(targetUserId);
            _friendshipRepositoryMock.Verify(repo => repo.UpdateAsync(existingFriendship), Times.Once);
        }
    }
}
