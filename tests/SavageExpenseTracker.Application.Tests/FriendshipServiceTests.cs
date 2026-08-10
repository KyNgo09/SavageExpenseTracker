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

        [Fact]
        public async Task SendRequestAsync_ShouldThrowException_WhenAlreadyAccepted()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(new Friendship { Status = "accepted" });

            Func<Task> act = async () => await _friendshipService.SendRequestAsync(currentUserId, targetUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Friendship already exists.");
        }

        [Fact]
        public async Task SendRequestAsync_ShouldThrowException_WhenBlocked()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(new Friendship { Status = "blocked" });

            Func<Task> act = async () => await _friendshipService.SendRequestAsync(currentUserId, targetUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You have been blocked by this user.");
        }

        [Fact]
        public async Task RejectRequestAsync_ShouldReturnFalse_WhenFriendshipNotFound()
        {
            _friendshipRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Friendship)null!);
            var result = await _friendshipService.RejectRequestAsync(Guid.NewGuid(), 1);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RejectRequestAsync_ShouldThrowException_WhenWrongUserOrStatus()
        {
            var currentUserId = Guid.NewGuid();
            var friendship = new Friendship { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid(), Status = "accepted" };
            _friendshipRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(friendship);

            Func<Task> act = async () => await _friendshipService.RejectRequestAsync(currentUserId, 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You do not have permission to reject this request.");
        }

        [Fact]
        public async Task RejectRequestAsync_ShouldDeleteAndReturnTrue_WhenSuccess()
        {
            var currentUserId = Guid.NewGuid();
            var friendship = new Friendship { FriendId = currentUserId, Status = "pending" };
            _friendshipRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(friendship);

            var result = await _friendshipService.RejectRequestAsync(currentUserId, 1);

            result.Should().BeTrue();
            _friendshipRepositoryMock.Verify(repo => repo.DeleteAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task UnfriendAsync_ShouldReturnFalse_WhenFriendshipNotFoundOrNotAccepted()
        {
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync((Friendship)null!);
            var result = await _friendshipService.UnfriendAsync(Guid.NewGuid(), Guid.NewGuid());
            result.Should().BeFalse();

            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(new Friendship { Status = "pending" });
            var result2 = await _friendshipService.UnfriendAsync(Guid.NewGuid(), Guid.NewGuid());
            result2.Should().BeFalse();
        }

        [Fact]
        public async Task UnfriendAsync_ShouldDeleteAndReturnTrue_WhenSuccess()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var friendship = new Friendship { Status = "accepted" };
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(friendship);

            var result = await _friendshipService.UnfriendAsync(currentUserId, targetUserId);

            result.Should().BeTrue();
            _friendshipRepositoryMock.Verify(repo => repo.DeleteAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldAddNewFriendship_WhenFriendshipDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync((Friendship)null!);

            var result = await _friendshipService.BlockUserAsync(currentUserId, targetUserId);

            result.Should().BeTrue();
            _friendshipRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Friendship>(f => 
                f.UserId == currentUserId && f.FriendId == targetUserId && f.Status == "blocked")), Times.Once);
        }

        [Fact]
        public async Task UnblockUserAsync_ShouldReturnFalse_WhenNotFoundOrNotBlockedOrWrongUser()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            // Not found
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync((Friendship)null!);
            (await _friendshipService.UnblockUserAsync(currentUserId, targetUserId)).Should().BeFalse();

            // Not blocked
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(new Friendship { Status = "accepted", UserId = currentUserId });
            (await _friendshipService.UnblockUserAsync(currentUserId, targetUserId)).Should().BeFalse();

            // Wrong user (current user was blocked by target, can't unblock)
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(new Friendship { Status = "blocked", UserId = targetUserId });
            (await _friendshipService.UnblockUserAsync(currentUserId, targetUserId)).Should().BeFalse();
        }

        [Fact]
        public async Task UnblockUserAsync_ShouldDeleteAndReturnTrue_WhenSuccess()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var friendship = new Friendship { Status = "blocked", UserId = currentUserId };
            
            _friendshipRepositoryMock.Setup(repo => repo.GetFriendshipBetweenAsync(currentUserId, targetUserId))
                .ReturnsAsync(friendship);

            var result = await _friendshipService.UnblockUserAsync(currentUserId, targetUserId);

            result.Should().BeTrue();
            _friendshipRepositoryMock.Verify(repo => repo.DeleteAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task GetFriendsListAsync_ShouldReturnPagedResultDto()
        {
            var currentUserId = Guid.NewGuid();
            var friendId = Guid.NewGuid();
            var friendships = new List<Friendship>
            {
                new Friendship { UserId = currentUserId, FriendId = friendId, Friend = new User { Id = friendId, UserName = "friend1" } }
            };

            _friendshipRepositoryMock.Setup(repo => repo.GetFriendsAsync(currentUserId, 1, 10)).ReturnsAsync((friendships, 1));

            var result = await _friendshipService.GetFriendsListAsync(currentUserId, 1, 10);

            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().UserName.Should().Be("friend1");
        }

        [Fact]
        public async Task GetPendingRequestsAsync_ShouldReturnPagedResultDto()
        {
            var currentUserId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var friendships = new List<Friendship>
            {
                new Friendship { Id = 1, User = new User { Id = senderId, UserName = "sender1" }, CreatedAt = DateTime.UtcNow }
            };

            _friendshipRepositoryMock.Setup(repo => repo.GetPendingRequestsAsync(currentUserId, 1, 10)).ReturnsAsync((friendships, 1));

            var result = await _friendshipService.GetPendingRequestsAsync(currentUserId, 1, 10);

            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().SenderName.Should().Be("sender1");
        }
    }
}
