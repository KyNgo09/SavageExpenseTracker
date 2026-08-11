using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Extensions;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/friendships")]
    [Authorize]
    public class FriendshipsController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipsController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        // GET: api/friendships/friends?pageNumber=1&pageSize=10
        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var pagedFriends = await _friendshipService.GetFriendsListAsync(User.GetUserId(), pageNumber, pageSize);
            return Ok(pagedFriends);
        }

        // GET: api/friendships/requests?pageNumber=1&pageSize=10
        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var pagedRequests = await _friendshipService.GetPendingRequestsAsync(User.GetUserId(), pageNumber, pageSize);
            return Ok(pagedRequests);
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendRequest(Guid targetUserId)
        {
            await _friendshipService.SendRequestAsync(User.GetUserId(), targetUserId);
            return Ok(new { Message = "Friend request sent." });
        }

        [HttpPut("accept/{id}")]
        public async Task<IActionResult> AcceptRequest(long id)
        {
            var success = await _friendshipService.AcceptRequestAsync(User.GetUserId(), id);
            if (!success) return NotFound();
            return Ok(new { Message = "Friend request accepted." });
        }

        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> RejectRequest(long id)
        {
            var success = await _friendshipService.RejectRequestAsync(User.GetUserId(), id);
            if (!success) return NotFound();
            return Ok(new { Message = "Friend request rejected." });
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> Unfriend(Guid friendId)
        {
            var success = await _friendshipService.UnfriendAsync(User.GetUserId(), friendId);
            if (!success) return NotFound();
            return Ok(new { Message = "Friendship removed." });
        }

        [HttpPost("block/{targetUserId}")]
        public async Task<IActionResult> BlockUser(Guid targetUserId)
        {
            await _friendshipService.BlockUserAsync(User.GetUserId(), targetUserId);
            return Ok(new { Message = "User blocked." });
        }

        [HttpDelete("unblock/{targetUserId}")]
        public async Task<IActionResult> UnblockUser(Guid targetUserId)
        {
            var success = await _friendshipService.UnblockUserAsync(User.GetUserId(), targetUserId);
            if (!success) return NotFound();
            return Ok(new { Message = "User unblocked." });
        }
    }
}