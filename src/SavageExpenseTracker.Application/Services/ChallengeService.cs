using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IExpenseRepository _expenseRepository;
        private readonly IChallengeNotificationService _notificationService;

        public ChallengeService(
            IChallengeRepository challengeRepository, 
            IExpenseRepository expenseRepository, 
            IChallengeNotificationService notificationService)   
        {
            _challengeRepository = challengeRepository;
            _expenseRepository = expenseRepository;
            _notificationService = notificationService;
        }
        
        public async Task<IEnumerable<ChallengeDto>> GetAllChallengesAsync()
        {
            var challenges = await _challengeRepository.GetAllAsync();
            return challenges.Select(c => c.ToDto());
        }    

        public async Task<ChallengeDto?> GetChallengeByIdAsync(long id)
        {
            var challenge = await _challengeRepository.GetByIdAsync(id);
            return challenge?.ToDto();
        }

        public async Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto createCategoryDto)
        {
            var challenge = new Challenge
            {
                Name = createCategoryDto.Name,
                DateStart = createCategoryDto.DateStart.ToUniversalTime(),
                DateEnd = createCategoryDto.DateEnd.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow
            };
            await _challengeRepository.AddAsync(challenge);
            return challenge.ToDto();
        }

        public async Task<bool> JoinChallengeAsync(long challengeId, Guid userId)
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge == null) return false;

            if(DateTime.UtcNow > challenge.DateEnd)
                throw new InvalidOperationException("Challenge has ended!");

            if(challenge.ChallengeMembers.Any(m => m.UserId == userId))    
                throw new InvalidOperationException("User has already joined this challenge!");

            var member = new ChallengeMember
            {
                ChallengeId = challengeId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            await _challengeRepository.AddMemberAsync(member);
            await _notificationService.NotifyUserJoinedAsync(challengeId, userId);
            return true;
        }

        public async Task<bool> LeaveChallengeAsync(long challengeId, Guid userId)
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if(challenge == null) return false;

            var member = challenge.ChallengeMembers.FirstOrDefault(m => m.UserId == userId);
            if(member == null) return false;

            if(DateTime.UtcNow > challenge.DateStart)
                throw new InvalidOperationException("Challenge has already started!");

            await _challengeRepository.RemovemMemberAsync(member);
            await _notificationService.NotifyUserLeftAsync(challengeId, userId);
            return true;
        }

        public async Task<IEnumerable<LeaderboardItemDto>> GetLeaderboardAsync(long challengeId)
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge == null) return new List<LeaderboardItemDto>();

            var leaderboard = new List<LeaderboardItemDto>();

            foreach (var member in challenge.ChallengeMembers)
            {
                var expenses = await _expenseRepository.GetByUserIdAsync(member.UserId);
                
                // Filter Expense within the challenge timeframe.
                var validExpenses = expenses.Where(e => e.CreatedAt >= challenge.DateStart && e.CreatedAt <= challenge.DateEnd).ToList();
                
                var totalAmount = validExpenses.Sum(e => e.Amount);
                var totalTimeWork = validExpenses.Sum(e => e.TimeWork);

                leaderboard.Add(new LeaderboardItemDto
                {
                    UserId = member.UserId,
                    TotalAmount = totalAmount,
                    TotalTimeWork = totalTimeWork,
                    Title = string.Empty 
                });
            }

            // Titles are awarded based on relative comparisons rather than specific numbers.
            if (leaderboard.Count > 0)
            {
                var maxAmount = leaderboard.Max(x => x.TotalAmount);
                var minAmount = leaderboard.Min(x => x.TotalAmount);

                foreach (var item in leaderboard)
                {
                    if (maxAmount > minAmount) // Must have a difference to be assigned
                    {
                        if (item.TotalAmount == maxAmount) 
                            item.Title = "Báo Thủ"; // Most money spent
                        else if (item.TotalAmount == minAmount) 
                            item.Title = "Thánh Sinh Tồn"; // Least money spent
                    }
                    else if (leaderboard.Count == 1)
                    {
                        // If only one person in the room, automatically becomes the survivor
                        item.Title = "Thánh Sinh Tồn";
                    }
                }
            }

            return leaderboard.OrderBy(x => x.TotalAmount); // Sort by amount (who spends less is top 1)
        }

        public async Task ProcessExpiredChallengesAsync()
        {
            var finishedChallenges = await _challengeRepository.GetUnprocessedFinishedChallengesAsync();

            foreach (var challenge in finishedChallenges)
            {
                if (challenge.ChallengeMembers.Count > 0)
                {
                    var leaderboard = await GetLeaderboardAsync(challenge.Id);
                    
                    var winner = leaderboard.OrderBy(l => l.TotalAmount).First();
                    var loser = leaderboard.OrderByDescending(l => l.TotalAmount).First();

                    challenge.WinnerId = winner.UserId;
                    challenge.LoserId = loser.UserId;

                    await _challengeRepository.UpdateAsync(challenge);
                    
                    await _notificationService.NotifyChallengeEndedAsync(challenge.Id, winner.UserId, loser.UserId);
                }
            }
        }
    }
}