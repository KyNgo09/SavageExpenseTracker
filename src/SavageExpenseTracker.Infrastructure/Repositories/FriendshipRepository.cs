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
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;
        public FriendshipRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<Friendship?> GetByIdAsync(long id)
        {
            return await _context.Friendships.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Friendship?> GetFriendshipBetweenAsync(Guid userA, Guid userB)
        {
            return await _context.Friendships.FirstOrDefaultAsync(f => (f.UserId == userA && f.FriendId == userB) || (f.UserId == userB && f.FriendId == userA));
        }

        public async Task AddAsync(Friendship friendship)
        {
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Friendship friendship)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Friendship friendship)
        {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId)
        {
            return await _context.Friendships.Include(f => f.User).Include(f => f.Friend).Where(f => f.Status == "accepted" && (f.UserId == userId || f.FriendId == userId)).ToListAsync();
        }

        public async Task<(IEnumerable<Friendship> Items, int TotalCount)> GetFriendsPagedAsync(Guid userId, int pageNumber, int pageSize)
        {
            var query = _context.Friendships
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Friend)
                .Where(f => f.Status == "accepted" && (f.UserId == userId || f.FriendId == userId));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId)
        {
            return await _context.Friendships.Include(f => f.User).Where(f => f.FriendId == userId && f.Status == "pending").ToListAsync();
        }

        public async Task<(IEnumerable<Friendship> Items, int TotalCount)> GetPendingRequestsPagedAsync(Guid userId, int pageNumber, int pageSize)
        {
            var query = _context.Friendships
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => f.FriendId == userId && f.Status == "pending");

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}