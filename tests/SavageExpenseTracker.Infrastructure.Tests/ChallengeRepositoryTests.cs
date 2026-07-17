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
    public class ChallengeRepositoryTests : IDisposable
    {
        private readonly SavageExpenseTrackerDbContext _context;
        private readonly ChallengeRepository _repository;

        public ChallengeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SavageExpenseTrackerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SavageExpenseTrackerDbContext(options);
            _repository = new ChallengeRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AddAsync_ShouldAddChallenge()
        {
            var challenge = new Challenge { Name = "Test Challenge" };
            await _repository.AddAsync(challenge);

            var saved = await _context.Challenges.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Test Challenge");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllChallenges()
        {
            await _context.Challenges.AddAsync(new Challenge { Name = "C1" });
            await _context.Challenges.AddAsync(new Challenge { Name = "C2" });
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnChallenge_WithMembers()
        {
            var challenge = new Challenge { Name = "C1" };
            await _context.Challenges.AddAsync(challenge);
            await _context.SaveChangesAsync();
            await _context.ChallengeMembers.AddAsync(new ChallengeMember { ChallengeId = challenge.Id, UserId = Guid.NewGuid() });
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(challenge.Id);
            result.Should().NotBeNull();
            result!.ChallengeMembers.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            var result = await _repository.GetByIdAsync(999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateChallenge()
        {
            var challenge = new Challenge { Name = "C1" };
            await _context.Challenges.AddAsync(challenge);
            await _context.SaveChangesAsync();

            challenge.Name = "Updated";
            await _repository.UpdateAsync(challenge);

            var result = await _context.Challenges.FindAsync(challenge.Id);
            result!.Name.Should().Be("Updated");
        }

        [Fact]
        public async Task AddMemberAsync_ShouldAddMemberToChallenge()
        {
            var member = new ChallengeMember { ChallengeId = 1, UserId = Guid.NewGuid() };
            await _repository.AddMemberAsync(member);

            var saved = await _context.ChallengeMembers.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldRemoveMember()
        {
            var member = new ChallengeMember { ChallengeId = 1, UserId = Guid.NewGuid() };
            await _context.ChallengeMembers.AddAsync(member);
            await _context.SaveChangesAsync();

            await _repository.RemovemMemberAsync(member);

            var saved = await _context.ChallengeMembers.FirstOrDefaultAsync();
            saved.Should().BeNull();
        }

        [Fact]
        public async Task GetUnprocessedFinishedChallengesAsync_ShouldReturnUnprocessedChallenges()
        {
            var oldChallenge = new Challenge { Name = "Old", DateEnd = DateTime.UtcNow.AddDays(-1), WinnerId = null };
            var activeChallenge = new Challenge { Name = "Active", DateEnd = DateTime.UtcNow.AddDays(1), WinnerId = null };
            var processedChallenge = new Challenge { Name = "Processed", DateEnd = DateTime.UtcNow.AddDays(-1), WinnerId = Guid.NewGuid() };
            
            await _context.Challenges.AddRangeAsync(oldChallenge, activeChallenge, processedChallenge);
            await _context.SaveChangesAsync();

            var result = await _repository.GetUnprocessedFinishedChallengesAsync();
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Old");
        }
    }
}
