using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.Infrastructure.Services
{
    public class SavageCommentBackgroundWorker : BackgroundService
    {
        private readonly ISavageCommentQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SavageCommentBackgroundWorker> _logger;

        public SavageCommentBackgroundWorker(ISavageCommentQueue queue, IServiceScopeFactory scopeFactory, ILogger<SavageCommentBackgroundWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Savage Comment Background Worker is starting.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var expenseId = await _queue.DequeueAsync(stoppingToken);
                    using var scope = _scopeFactory.CreateScope();
                    var expenseRepository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();
                    var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
                    var aiService = scope.ServiceProvider.GetRequiredService<ISavageAiService>();
                    
                    var expense = await expenseRepository.GetByIdAsync(expenseId);
                    if (expense == null) continue;

                    var category = await categoryRepository.GetByIdAsync(expense.CategoryId);
                    var categoryName = category?.Name ?? "Chưa phân loại";

                    var savageComment = await aiService.GenerateSavageCommentAsync(expense.Description, expense.Amount, expense.TimeWork, categoryName);

                    expense.SavageComment = savageComment;
                    await expenseRepository.UpdateAsync(expense);

                    _logger.LogInformation("Generated savage comment for expense: {ExpenseId}", expenseId);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating savage comment for expense: {Message}", ex.Message);
                }
            }
        }
    }
}