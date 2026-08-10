using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IChallengeRepository
    {
        Task<IEnumerable<Challenge>> GetAllAsync();
        Task<(IEnumerable<Challenge> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<Challenge?> GetByIdAsync(long id);
        Task AddAsync(Challenge challenge);
        Task UpdateAsync(Challenge challenge);
        Task AddMemberAsync(ChallengeMember member);
        Task RemovemMemberAsync(ChallengeMember member);
        Task<IEnumerable<Challenge>> GetUnprocessedFinishedChallengesAsync(); // CronJob  
    }
}