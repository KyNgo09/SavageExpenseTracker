using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Friendship;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;

        public FriendshipService(IFriendshipRepository friendshipRepository)
        {
            _friendshipRepository = friendshipRepository;
        }

        public async Task<bool> SendRequestAsync(Guid currentUserId, Guid targetUserId)
        {
            if (currentUserId == targetUserId)
                throw new InvalidOperationException("You cannot send a friend request to yourself.");

            var existingFriendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);
            if (existingFriendship != null)
            {
                switch (existingFriendship.Status)
                {
                    case "pending":
                        throw new InvalidOperationException("Friend request is already pending.");
                    case "accepted":
                        throw new InvalidOperationException("Friendship already exists.");
                    case "blocked":
                        throw new InvalidOperationException("You have been blocked by this user.");
                    default:
                        throw new InvalidOperationException("Invalid friendship status.");
                } 
            }

            var friendship = new Friendship
            {
                UserId = currentUserId,
                FriendId = targetUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipRepository.AddAsync(friendship);
            return true;
        }

        public async Task<bool> AcceptRequestAsync(Guid currentUserId, long friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
            if (friendship == null) return false;

            if(friendship.FriendId != currentUserId || friendship.Status != "pending")
                throw new InvalidOperationException("You do not have permission to accept this request.");
            
            friendship.Status = "accepted";
            await _friendshipRepository.UpdateAsync(friendship);
            return true;
        }

        public async Task<bool> RejectRequestAsync(Guid currentUserId, long friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
            if (friendship == null) return false;

            if((friendship.FriendId == currentUserId || friendship.UserId == currentUserId) && friendship.Status == "pending")
            {
                await _friendshipRepository.DeleteAsync(friendship);
                return true;
            }

            throw new InvalidOperationException("You do not have permission to reject this request.");
        }

        public async Task<bool> UnfriendAsync(Guid currentUserId, Guid friendId)
        {
            var friendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, friendId);
            if (friendship == null || friendship.Status != "accepted") return false;

            await _friendshipRepository.DeleteAsync(friendship);
            return true;
        }

        public async Task<bool> BlockUserAsync(Guid currentUserId, Guid targetUserId)
        {
            if (currentUserId == targetUserId) 
                throw new InvalidOperationException("You cannot block yourself.");

            var existing = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);

            if (existing != null) 
            {
                existing.UserId = currentUserId;
                existing.FriendId = targetUserId;
                existing.Status = "blocked";
                await _friendshipRepository.UpdateAsync(existing);
            }
            else
            {
                var friendship = new Friendship
                {
                    UserId = currentUserId,
                    FriendId = targetUserId,
                    Status = "blocked",
                    CreatedAt = DateTime.UtcNow
                };
                await _friendshipRepository.AddAsync(friendship);
            }
            return true;
        }

        public async Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId)
        {
            var friendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);
            if (friendship == null || friendship.Status != "blocked" || friendship.UserId != currentUserId)
                return false;

            await _friendshipRepository.DeleteAsync(friendship);
            return true;
        }

        public async Task<IEnumerable<UserProfileDto>> GetFriendsListAsync(Guid currentUserId)
        {
           var friendships = await _friendshipRepository.GetFriendsAsync(currentUserId);

           return friendships.Select(f => 
           {
                var friendInfo = f.UserId == currentUserId ? f.Friend : f.User;
                return new UserProfileDto
                {
                    Id = friendInfo!.Id,
                    UserName = friendInfo.UserName,
                    Email = friendInfo.Email
                };
           });
        }

        public async Task<PagedResultDto<UserProfileDto>> GetFriendsListPagedAsync(Guid currentUserId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _friendshipRepository.GetFriendsPagedAsync(currentUserId, pageNumber, pageSize);

            var dtos = items.Select(f =>
            {
                var friendInfo = f.UserId == currentUserId ? f.Friend : f.User;
                return new UserProfileDto
                {
                    Id = friendInfo!.Id,
                    UserName = friendInfo.UserName,
                    Email = friendInfo.Email
                };
            });

            return new PagedResultDto<UserProfileDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<FriendshipRequestDto>> GetPendingRequestsAsync(Guid currentUserId)
        {
            var requests = await _friendshipRepository.GetPendingRequestsAsync(currentUserId);

            return requests.Select(f => new FriendshipRequestDto
            {
                FriendshipId = f.Id,
                SenderId = f.User!.Id,
                SenderName = f.User.UserName,
                SentAt = f.CreatedAt
            });
        }

        public async Task<PagedResultDto<FriendshipRequestDto>> GetPendingRequestsPagedAsync(Guid currentUserId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _friendshipRepository.GetPendingRequestsPagedAsync(currentUserId, pageNumber, pageSize);

            var dtos = items.Select(f => new FriendshipRequestDto
            {
                FriendshipId = f.Id,
                SenderId = f.User!.Id,
                SenderName = f.User.UserName,
                SentAt = f.CreatedAt
            });

            return new PagedResultDto<FriendshipRequestDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}