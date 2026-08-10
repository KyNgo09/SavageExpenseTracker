using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Friendship;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IFriendshipService
    {
        Task<bool> SendRequestAsync(Guid currentUserId, Guid targetUserId);
        Task<bool> AcceptRequestAsync(Guid currentUserId, long friendshipId);
        Task<bool> RejectRequestAsync(Guid currentUserId, long friendshipId);
        Task<bool> UnfriendAsync(Guid currentUserId, Guid friendId);
        Task<bool> BlockUserAsync(Guid currentUserId, Guid targetUserId);
        Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId);

        Task<IEnumerable<UserProfileDto>> GetFriendsListAsync(Guid currentUserId);
        Task<PagedResultDto<UserProfileDto>> GetFriendsListPagedAsync(Guid currentUserId, int pageNumber, int pageSize);
        Task<IEnumerable<FriendshipRequestDto>> GetPendingRequestsAsync(Guid currentUserId);
        Task<PagedResultDto<FriendshipRequestDto>> GetPendingRequestsPagedAsync(Guid currentUserId, int pageNumber, int pageSize);
    }
}