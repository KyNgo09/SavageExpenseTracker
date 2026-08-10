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

        Task<PagedResultDto<UserProfileDto>> GetFriendsListAsync(Guid currentUserId, int pageNumber, int pageSize);
        Task<PagedResultDto<FriendshipRequestDto>> GetPendingRequestsAsync(Guid currentUserId, int pageNumber, int pageSize);
    }
}