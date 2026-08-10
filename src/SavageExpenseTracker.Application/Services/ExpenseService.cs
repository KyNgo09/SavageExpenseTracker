using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly IUserRepository _userRepository;

        public ExpenseService(IExpenseRepository expenseRepository, IUserRepository userRepository)
        {
            _expenseRepository = expenseRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedResultDto<ExpenseDto>> GetUserExpensesAsync(Guid userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _expenseRepository.GetByUserIdAsync(userId, pageNumber, pageSize);

            return new PagedResultDto<ExpenseDto>
            {
                Items = items.Select(e => e.ToDto()),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<ExpenseDto?> GetExpenseByIdAsync(long id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            return expense?.ToDto();
        }
        public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto createExpenseDto)
        {
            var user = await _userRepository.GetByIdAsync(createExpenseDto.UserId);
            if(user == null)
            {
                throw new InvalidOperationException("User not found!");
            }

            var timeWork = user.HourlyRate > 0 ? createExpenseDto.Amount / user.HourlyRate : 0;

            var expense = new Expense
            {
                UserId = createExpenseDto.UserId,
                Description = createExpenseDto.Description,
                Amount = createExpenseDto.Amount,
                TimeWork = timeWork,
                CreatedAt = DateTime.UtcNow,
                AppliedHourlyRate = user.HourlyRate,
                CategoryId = createExpenseDto.CategoryId
            };

            await _expenseRepository.AddAsync(expense);
            return expense.ToDto();
        }

         public async Task<bool> UpdateExpenseAsync(long id, Guid currentUserId, UpdateExpenseDto updateExpenseDto)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return false;
            
            if (expense.UserId != currentUserId)
            {
                throw new InvalidOperationException("You do not have permission to update this expense.");
            }

            // Calculate TimeWork based on the applied hourly rate saved in the past (do not use the current user.HourlyRate)
            var timeWork = expense.AppliedHourlyRate > 0 ? updateExpenseDto.Amount / expense.AppliedHourlyRate : 0;
            expense.Description = updateExpenseDto.Description;
            expense.Amount = updateExpenseDto.Amount;
            expense.TimeWork = timeWork;
            expense.CategoryId = updateExpenseDto.CategoryId;
            
            // Do not assign ImageUrl and SavageComment from dto because client-side modifications via this API are not allowed.
            await _expenseRepository.UpdateAsync(expense);
            return true;
        }

        public async Task<bool> DeleteExpenseAsync(long id, Guid currentUserId)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return false;

            if (expense.UserId != currentUserId)
            {
                throw new InvalidOperationException("You do not have permission to delete this expense.");
            }
            await _expenseRepository.DeleteAsync(id);
            return true;
        }
    }   
}