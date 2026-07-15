using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Friendship;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class FriendshipServie : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;

        public FriendshipServie(IFriendshipRepository FriendshipRepository)
        {
            _friendshipRepository = FriendshipRepository;
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

        public async Task<bool> AcceptRequestAsync(Guid currentUserId, long )
    }
}