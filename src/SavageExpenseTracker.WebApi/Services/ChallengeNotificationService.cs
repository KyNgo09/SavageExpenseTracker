using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Hubs;

namespace SavageExpenseTracker.WebApi.Services
{
    public class ChallengeNotificationService : IChallengeNotificationService
    {
        private readonly IHubContext<ChallengeHub> _hubContext;

        public ChallengeNotificationService(IHubContext<ChallengeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyUserJoinedAsync(long challengeId, Guid userId)
        {
            await _hubContext.Clients.Group($"Challenge_{challengeId}").SendAsync("UserJoined", userId);
        }

        public async Task NotifyUserLeftAsync(long challengeId, Guid userId)
        {
            await _hubContext.Clients.Group($"Challenge_{challengeId}").SendAsync("UserLeft", userId);
        }

        public async Task NotifyChallengeEndedAsync(long challengeId, Guid winnerId, Guid loserId)
        {
            await _hubContext.Clients.Group($"Challenge_{challengeId}").SendAsync("ChallengeEnded", new { Winner = winnerId, Loser = loserId });
        }
    }
}
