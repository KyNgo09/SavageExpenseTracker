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
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public ExpenseRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Expense> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize)
        {
            var query = _context.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Expense?> GetByIdAsync(long id)
        {
            return await _context.Expenses.FindAsync(id);
        }

        public async Task AddAsync (Expense expense)
        {
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Expense expense)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var expense = await GetByIdAsync(id);
            if(expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Expense>> GetByUserAndDateRangeAsync(IEnumerable<Guid> userIds, DateTime startDate, DateTime endDate)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => userIds.Contains(e.UserId)
                    && e.CreatedAt >= startDate
                    && e.CreatedAt <= endDate
                )
                .ToListAsync();
        }
    }
}