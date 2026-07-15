using System;
using System.Threading.Tasks;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IChallengeNotificationService
    {
        Task NotifyUserJoinedAsync(long challengeId, Guid userId);
        Task NotifyUserLeftAsync(long challengeId, Guid userId);
        Task NotifyChallengeEndedAsync(long challengeId, Guid winnerId, Guid loserId);
    }
}