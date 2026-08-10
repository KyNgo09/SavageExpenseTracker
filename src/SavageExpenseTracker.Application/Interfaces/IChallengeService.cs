using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Challenge;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IChallengeService
    {
        Task<IEnumerable<ChallengeDto>> GetAllChallengesAsync();
        Task<PagedResultDto<ChallengeDto>> GetChallengesPagedAsync(int pageNumber, int pageSize);
        Task<ChallengeDto?> GetChallengeByIdAsync(long id);
        Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto createChallengeDto);
        Task<bool> JoinChallengeAsync(long challengeId, Guid userId);
        Task<bool> LeaveChallengeAsync(long challengeId, Guid userId);
        Task<IEnumerable<LeaderboardItemDto>> GetLeaderboardAsync(long challengeId);
        Task ProcessExpiredChallengesAsync();
    }
}