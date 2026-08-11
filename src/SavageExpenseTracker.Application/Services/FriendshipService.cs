using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Friendship;
using SavageExpenseTracker.Application.Helpers;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Constants;
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
                    case FriendshipStatus.Pending:
                        throw new InvalidOperationException("Friend request is already pending.");
                    case FriendshipStatus.Accepted:
                        throw new InvalidOperationException("Friendship already exists.");
                    case FriendshipStatus.Blocked:
                        throw new InvalidOperationException("You have been blocked by this user.");
                    default:
                        throw new InvalidOperationException("Invalid friendship status.");
                } 
            }

            var friendship = new Friendship
            {
                UserId = currentUserId,
                FriendId = targetUserId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipRepository.AddAsync(friendship);
            return true;
        }

        public async Task<bool> AcceptRequestAsync(Guid currentUserId, long friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
            if (friendship == null) return false;

            if (friendship.FriendId != currentUserId || friendship.Status != FriendshipStatus.Pending)
                throw new InvalidOperationException("You do not have permission to accept this request.");
            
            friendship.Status = FriendshipStatus.Accepted;
            await _friendshipRepository.UpdateAsync(friendship);
            return true;
        }

        public async Task<bool> RejectRequestAsync(Guid currentUserId, long friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
            if (friendship == null) return false;

            if ((friendship.FriendId == currentUserId || friendship.UserId == currentUserId) && friendship.Status == FriendshipStatus.Pending)
            {
                await _friendshipRepository.DeleteAsync(friendship);
                return true;
            }

            throw new InvalidOperationException("You do not have permission to reject this request.");
        }

        public async Task<bool> UnfriendAsync(Guid currentUserId, Guid friendId)
        {
            var friendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, friendId);
            if (friendship == null || friendship.Status != FriendshipStatus.Accepted) return false;

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
                existing.Status = FriendshipStatus.Blocked;
                await _friendshipRepository.UpdateAsync(existing);
            }
            else
            {
                var friendship = new Friendship
                {
                    UserId = currentUserId,
                    FriendId = targetUserId,
                    Status = FriendshipStatus.Blocked,
                    CreatedAt = DateTime.UtcNow
                };
                await _friendshipRepository.AddAsync(friendship);
            }
            return true;
        }

        public async Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId)
        {
            var friendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);
            if (friendship == null || friendship.Status != FriendshipStatus.Blocked || friendship.UserId != currentUserId)
                return false;

            await _friendshipRepository.DeleteAsync(friendship);
            return true;
        }

        public async Task<PagedResultDto<UserProfileDto>> GetFriendsListAsync(Guid currentUserId, int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var (items, totalCount) = await _friendshipRepository.GetFriendsAsync(currentUserId, pageNumber, pageSize);

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

        public async Task<PagedResultDto<FriendshipRequestDto>> GetPendingRequestsAsync(Guid currentUserId, int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var (items, totalCount) = await _friendshipRepository.GetPendingRequestsAsync(currentUserId, pageNumber, pageSize);

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