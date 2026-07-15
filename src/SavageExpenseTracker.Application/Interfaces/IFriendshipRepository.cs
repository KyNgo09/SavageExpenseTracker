using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IFriendshipRepository
    {
        Task<Friendship?> GetByIdAsync(long id);
        Task<Friendship?> GetFriendshipBetweenAsync(Guid userA, Guid userB);
        Task AddAsync(Friendship friendship);
        Task UpdateAsync(Friendship friendship);
        Task DeleteAsync(Friendship friendship);
        Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId);
        Task<IEnumerable<Friendship>> GetPendingRequestAsync(Guid userId);
    }
}