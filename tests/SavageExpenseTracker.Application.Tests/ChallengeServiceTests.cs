using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Domain.Entities;
using Xunit;

namespace SavageExpenseTracker.Application.Tests
{
    public class ChallengeServiceTests
    {
        private readonly Mock<IChallengeRepository> _challengeRepoMock;
        private readonly Mock<IExpenseRepository> _expenseRepoMock;
        private readonly Mock<IChallengeNotificationService> _notificationMock;
        private readonly ChallengeService _service;

        public ChallengeServiceTests()
        {
            _challengeRepoMock = new Mock<IChallengeRepository>();
            _expenseRepoMock = new Mock<IExpenseRepository>();
            _notificationMock = new Mock<IChallengeNotificationService>();
            _service = new ChallengeService(_challengeRepoMock.Object, _expenseRepoMock.Object, _notificationMock.Object);
        }

        [Fact]
        public async Task GetAllChallengesAsync_ShouldReturnPagedResultDto()
        {
            _challengeRepoMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((new List<Challenge> { new Challenge { Id = 1 } }, 1));
            var result = await _service.GetAllChallengesAsync(1, 10);
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetChallengeByIdAsync_ShouldReturnDto()
        {
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Challenge { Id = 1 });
            var result = await _service.GetChallengeByIdAsync(1);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetChallengeByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Challenge)null!);
            var result = await _service.GetChallengeByIdAsync(1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateChallengeAsync_ShouldAddAndReturnDto()
        {
            var dto = new CreateChallengeDto { Name = "Save", DateStart = DateTime.UtcNow, DateEnd = DateTime.UtcNow.AddDays(7) };
            var result = await _service.CreateChallengeAsync(dto);
            result.Should().NotBeNull();
            _challengeRepoMock.Verify(r => r.AddAsync(It.IsAny<Challenge>()), Times.Once);
        }

        [Fact]
        public async Task JoinChallengeAsync_ShouldReturnFalse_WhenNotFound()
        {
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Challenge)null!);
            var result = await _service.JoinChallengeAsync(1, Guid.NewGuid());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task JoinChallengeAsync_ShouldThrowException_WhenEnded()
        {
            var challenge = new Challenge { Id = 1, DateEnd = DateTime.UtcNow.AddDays(-1) };
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            
            Func<Task> act = async () => await _service.JoinChallengeAsync(1, Guid.NewGuid());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Challenge has ended!");
        }

        [Fact]
        public async Task JoinChallengeAsync_ShouldThrowException_WhenAlreadyJoined()
        {
            var userId = Guid.NewGuid();
            var challenge = new Challenge 
            { 
                Id = 1, DateEnd = DateTime.UtcNow.AddDays(1), 
                ChallengeMembers = new List<ChallengeMember> { new ChallengeMember { UserId = userId } } 
            };
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            Func<Task> act = async () => await _service.JoinChallengeAsync(1, userId);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User has already joined this challenge!");
        }

        [Fact]
        public async Task JoinChallengeAsync_ShouldAddMemberAndNotify()
        {
            var userId = Guid.NewGuid();
            var challenge = new Challenge { Id = 1, DateEnd = DateTime.UtcNow.AddDays(1) };
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.JoinChallengeAsync(1, userId);
            
            result.Should().BeTrue();
            _challengeRepoMock.Verify(r => r.AddMemberAsync(It.Is<ChallengeMember>(m => m.UserId == userId)), Times.Once);
            _notificationMock.Verify(n => n.NotifyUserJoinedAsync(1, userId), Times.Once);
        }

        [Fact]
        public async Task LeaveChallengeAsync_ShouldReturnFalse_WhenNotFound()
        {
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Challenge)null!);
            var result = await _service.LeaveChallengeAsync(1, Guid.NewGuid());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task LeaveChallengeAsync_ShouldReturnFalse_WhenNotJoined()
        {
            var challenge = new Challenge { Id = 1 };
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            var result = await _service.LeaveChallengeAsync(1, Guid.NewGuid());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task LeaveChallengeAsync_ShouldThrowException_WhenAlreadyStarted()
        {
            var userId = Guid.NewGuid();
            var challenge = new Challenge 
            { 
                Id = 1, DateStart = DateTime.UtcNow.AddDays(-1), 
                ChallengeMembers = new List<ChallengeMember> { new ChallengeMember { UserId = userId } } 
            };
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            Func<Task> act = async () => await _service.LeaveChallengeAsync(1, userId);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Challenge has already started!");
        }

        [Fact]
        public async Task LeaveChallengeAsync_ShouldRemoveMemberAndNotify()
        {
            var userId = Guid.NewGuid();
            var member = new ChallengeMember { UserId = userId };
            var challenge = new Challenge 
            { 
                Id = 1, DateStart = DateTime.UtcNow.AddDays(1), 
                ChallengeMembers = new List<ChallengeMember> { member } 
            };
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.LeaveChallengeAsync(1, userId);
            
            result.Should().BeTrue();
            _challengeRepoMock.Verify(r => r.RemoveMemberAsync(member), Times.Once);
            _notificationMock.Verify(n => n.NotifyUserLeftAsync(1, userId), Times.Once);
        }

        [Fact]
        public async Task ProcessExpiredChallengesAsync_ShouldUpdateWinnersAndNotify()
        {
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var challenge = new Challenge 
            { 
                Id = 1, DateStart = DateTime.UtcNow.AddDays(-7), DateEnd = DateTime.UtcNow.AddDays(-1),
                ChallengeMembers = new List<ChallengeMember> 
                { 
                    new ChallengeMember { UserId = userId1 },
                    new ChallengeMember { UserId = userId2 }
                }
            };

            _challengeRepoMock.Setup(r => r.GetUnprocessedFinishedChallengesAsync()).ReturnsAsync(new List<Challenge> { challenge });
            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            // Setup expenses for leaderboard logic
            var ex1 = new Expense { UserId = userId1, Amount = 100, CreatedAt = DateTime.UtcNow.AddDays(-3) };
            var ex2 = new Expense { UserId = userId2, Amount = 200, CreatedAt = DateTime.UtcNow.AddDays(-3) };

            _expenseRepoMock.Setup(r => r.GetByUserAndDateRangeAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Expense> { ex1, ex2 });

            await _service.ProcessExpiredChallengesAsync();

            _challengeRepoMock.Verify(r => r.UpdateAsync(It.Is<Challenge>(c => c.WinnerId == userId1 && c.LoserId == userId2)), Times.Once);
            _notificationMock.Verify(n => n.NotifyChallengeEndedAsync(1, userId1, userId2), Times.Once);
        }

        [Fact]
        public async Task ProcessExpiredChallengesAsync_ShouldSkip_WhenNoMembers()
        {
            var challenge = new Challenge 
            { 
                Id = 1, DateStart = DateTime.UtcNow.AddDays(-7), DateEnd = DateTime.UtcNow.AddDays(-1),
                ChallengeMembers = new List<ChallengeMember>() // No members
            };

            _challengeRepoMock.Setup(r => r.GetUnprocessedFinishedChallengesAsync()).ReturnsAsync(new List<Challenge> { challenge });

            await _service.ProcessExpiredChallengesAsync();

            _challengeRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Challenge>()), Times.Never);
            _notificationMock.Verify(n => n.NotifyChallengeEndedAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetLeaderboardAsync_ShouldReturnThanhSinhTon_WhenOnlyOneMember()
        {
            var userId = Guid.NewGuid();
            var challenge = new Challenge 
            { 
                Id = 1, DateStart = DateTime.UtcNow.AddDays(-7), DateEnd = DateTime.UtcNow.AddDays(-1),
                ChallengeMembers = new List<ChallengeMember> { new ChallengeMember { UserId = userId } }
            };

            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            _expenseRepoMock.Setup(r => r.GetByUserAndDateRangeAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Expense>());

            var result = await _service.GetLeaderboardAsync(1);

            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Thánh Sinh Tồn");
        }

        [Fact]
        public async Task GetLeaderboardAsync_ShouldAssignTitlesCorrectly_AndFilterByDate()
        {
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var userId3 = Guid.NewGuid();
            var dateStart = DateTime.UtcNow.AddDays(-7);
            var dateEnd = DateTime.UtcNow.AddDays(-1);

            var challenge = new Challenge 
            { 
                Id = 1, DateStart = dateStart, DateEnd = dateEnd,
                ChallengeMembers = new List<ChallengeMember> 
                { 
                    new ChallengeMember { UserId = userId1 },
                    new ChallengeMember { UserId = userId2 },
                    new ChallengeMember { UserId = userId3 }
                }
            };

            _challengeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var ex1 = new Expense { UserId = userId1, Amount = 100, CreatedAt = dateStart.AddDays(1) };
            var ex2 = new Expense { UserId = userId2, Amount = 500, CreatedAt = dateStart.AddDays(1) };
            var ex3 = new Expense { UserId = userId3, Amount = 200, CreatedAt = dateStart.AddDays(1) };

            _expenseRepoMock.Setup(r => r.GetByUserAndDateRangeAsync(It.IsAny<IEnumerable<Guid>>(), dateStart, dateEnd))
                .ReturnsAsync(new List<Expense> { ex1, ex2, ex3 });

            var result = (await _service.GetLeaderboardAsync(1)).ToList();

            result.Should().HaveCount(3);
            
            // Order should be ascending by TotalAmount
            result[0].UserId.Should().Be(userId1); // 100
            result[0].Title.Should().Be("Thánh Sinh Tồn");

            result[1].UserId.Should().Be(userId3); // 200
            result[1].Title.Should().Be(string.Empty);

            result[2].UserId.Should().Be(userId2); // 500
            result[2].Title.Should().Be("Báo Thủ");
        }
    }
}
