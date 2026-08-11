using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SavageExpenseTracker.WebApi.Hubs
{
    public class ChallengeHub : Hub
    {
        public async Task JoinRoom(string challengeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Challenge_{challengeId}");
        }

        public async Task LeaveRoom(string challengeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Challenge_{challengeId}");
        }
    }
}