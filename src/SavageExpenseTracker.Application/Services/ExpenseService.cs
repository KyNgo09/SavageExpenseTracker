using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Constants;
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
        private readonly IPhotoService _photoService;
        private readonly ISavageCommentQueue _savageCommentQueue;

        public ExpenseService(IExpenseRepository expenseRepository, IUserRepository userRepository, IPhotoService photoService, ISavageCommentQueue savageCommentQueue)
        {
            _expenseRepository = expenseRepository;
            _userRepository = userRepository;
            _photoService = photoService;
            _savageCommentQueue = savageCommentQueue;
        }

        public async Task<PagedResultDto<ExpenseDto>> GetUserExpensesAsync(Guid userId, int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

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
            if (user == null)
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
                CategoryId = createExpenseDto.CategoryId,
                ImageUrl = createExpenseDto.ImageUrl
            };

            await _expenseRepository.AddAsync(expense);
            
            _savageCommentQueue.Enqueue(expense.Id);
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

            var timeWork = expense.AppliedHourlyRate > 0 ? updateExpenseDto.Amount / expense.AppliedHourlyRate : 0;
            expense.Description = updateExpenseDto.Description;
            expense.Amount = updateExpenseDto.Amount;
            expense.TimeWork = timeWork;
            expense.CategoryId = updateExpenseDto.CategoryId;
            
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

        public async Task<string> UploadReceiptAsync(System.IO.Stream stream, string fileName, string contentType, long fileLength)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic" };
            FileUploadHelper.ValidateImageFile(stream, fileName, contentType, fileLength, 10 * 1024 * 1024, allowedExtensions);

            return await _photoService.UploadPhotoAsync(stream, fileName, PhotoFolders.ExpensePhotos);
        }
    }
}