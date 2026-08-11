using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;
using SavageExpenseTracker.Infrastructure.Repositories;
using Xunit;

namespace SavageExpenseTracker.Infrastructure.Tests
{
    public class FriendshipRepositoryTests : IDisposable
    {
        private readonly SavageExpenseTrackerDbContext _context;
        private readonly FriendshipRepository _repository;

        public FriendshipRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SavageExpenseTrackerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SavageExpenseTrackerDbContext(options);
            _repository = new FriendshipRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AddAsync_ShouldAddFriendship()
        {
            var friendship = new Friendship { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid() };
            await _repository.AddAsync(friendship);

            var saved = await _context.Friendships.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnFriendship()
        {
            var friendship = new Friendship { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid() };
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(friendship.Id);
            result.Should().NotBeNull();
            result!.Id.Should().Be(friendship.Id);
        }

        [Fact]
        public async Task GetFriendshipBetweenAsync_ShouldReturnFriendship_RegardlessOfDirection()
        {
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();
            var friendship = new Friendship { UserId = userA, FriendId = userB };
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();

            var result1 = await _repository.GetFriendshipBetweenAsync(userA, userB);
            var result2 = await _repository.GetFriendshipBetweenAsync(userB, userA);

            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!.Id.Should().Be(friendship.Id);
            result2!.Id.Should().Be(friendship.Id);
        }

        [Fact]
        public async Task GetFriendshipBetweenAsync_ShouldReturnNull_WhenNotFound()
        {
            var result = await _repository.GetFriendshipBetweenAsync(Guid.NewGuid(), Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFriendship()
        {
            var friendship = new Friendship { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid(), Status = "pending" };
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();

            friendship.Status = "accepted";
            await _repository.UpdateAsync(friendship);

            var updated = await _context.Friendships.FindAsync(friendship.Id);
            updated!.Status.Should().Be("accepted");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveFriendship()
        {
            var friendship = new Friendship { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid() };
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(friendship);

            var deleted = await _context.Friendships.FirstOrDefaultAsync();
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task GetFriendsAsync_ShouldReturnAcceptedFriendships()
        {
            var userId = Guid.NewGuid();
            var friend1 = Guid.NewGuid();
            var friend2 = Guid.NewGuid();
            var friend3 = Guid.NewGuid();

            await _context.Users.AddRangeAsync(
                new User { Id = userId, UserName = "u0", Email="u0@t.com", PasswordHash="p" },
                new User { Id = friend1, UserName = "u1", Email="u1@t.com", PasswordHash="p" },
                new User { Id = friend2, UserName = "u2", Email="u2@t.com", PasswordHash="p" },
                new User { Id = friend3, UserName = "u3", Email="u3@t.com", PasswordHash="p" }
            );

            await _context.Friendships.AddAsync(new Friendship { UserId = userId, FriendId = friend1, Status = "accepted" });
            await _context.Friendships.AddAsync(new Friendship { UserId = friend2, FriendId = userId, Status = "accepted" });
            await _context.Friendships.AddAsync(new Friendship { UserId = userId, FriendId = friend3, Status = "pending" });
            await _context.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetFriendsAsync(userId, 1, 10);
            items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPendingRequestsAsync_ShouldReturnPendingRequestsForUser()
        {
            var userId = Guid.NewGuid();
            var friend1 = Guid.NewGuid();
            var friend2 = Guid.NewGuid();
            var friend3 = Guid.NewGuid();

            await _context.Users.AddRangeAsync(
                new User { Id = userId, UserName = "u0", Email="u0@t.com", PasswordHash="p" },
                new User { Id = friend1, UserName = "u1", Email="u1@t.com", PasswordHash="p" },
                new User { Id = friend2, UserName = "u2", Email="u2@t.com", PasswordHash="p" },
                new User { Id = friend3, UserName = "u3", Email="u3@t.com", PasswordHash="p" }
            );

            await _context.Friendships.AddAsync(new Friendship { UserId = friend1, FriendId = userId, Status = "pending" });
            await _context.Friendships.AddAsync(new Friendship { UserId = friend2, FriendId = userId, Status = "accepted" });
            await _context.Friendships.AddAsync(new Friendship { UserId = userId, FriendId = friend3, Status = "pending" }); // Not a request received by userId
            await _context.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPendingRequestsAsync(userId, 1, 10);
            items.Should().HaveCount(1);
        }
    }
}
