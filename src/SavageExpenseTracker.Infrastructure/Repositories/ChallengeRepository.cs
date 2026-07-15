using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;

namespace SavageExpenseTracker.Infrastructure.Repositories
{
    public class ChallengeRepository : IChallengeRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public ChallengeRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Challenge>> GetAllAsync()
        {
            return await _context.Challenges.ToListAsync();
        }

        public async Task<Challenge?> GetByIdAsync(long id)
        {
            return await _context.Challenges.Include(c => c.ChallengeMembers).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Challenge challenge)
        {
            await _context.Challenges.AddAsync(challenge);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Challenge challenge)
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddMemberAsync(ChallengeMember member)
        {
            await _context.ChallengeMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task RemovemMemberAsync(ChallengeMember member)
        {
            _context.ChallengeMembers.Remove(member);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Challenge>> GetUnprocessedFinishedChallengesAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Challenges
            .Include(c => c.ChallengeMembers)
            .Where(c => c.DateEnd < now && c.WinnerId == null)
            .ToListAsync();
        }
    }
}