using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;

namespace SavageExpenseTracker.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public UserRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var query = _context.Users.AsNoTracking();
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUserNameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await GetByIdAsync(id);
            if(user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> UserNameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<(IEnumerable<User> Items, int TotalCount)> SearchByEmailAsync(string query, int pageNumber, int pageSize)
        {
            var dbQuery = _context.Users
                .AsNoTracking()
                .Where(u => EF.Functions.ILike(u.Email, $"%{query}%"));

            var totalCount = await dbQuery.CountAsync();
            var items = await dbQuery
                .OrderBy(u => u.Email)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
