using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("/api/friendships")]
    [Authorize]
    public class FriendshipController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.Name.Identifier);

            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            
            throw new InvalidOperationException("Unable to determine current user."); 
        }

        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends()
        {
            var friends = await _friendshipService.GetFriendsListAsync(GetCurrentUserId());
            return Ok(friends);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _friendshipService.GetPendingRequestsAsync(GetCurrentUserId());
            return Ok(requests);
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendRequest(Guid targetUserId)
        {
            try
            {
                await _friendshipService.SendRequestAsync(GetCurrentUserId(), targetUserId);
                return Ok(new { Message = "Friend request sent." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("accept/{id}")]
        public async Task<IActionResult> AcceptRequest(long id)
        {
            try
            {
                var success = await _friendshipService.AcceptRequestAsync(GetCurrentUserId(), id);
                if (!success) return NotFound();
                return Ok(new { Message = "Friend request accepted." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> RejectRequest(long id)
        {
            try
            {
                var success = await _friendshipService.RejectRequestAsync(GetCurrentUserId(), id);
                if (!success) return NotFound();
                return Ok(new { Message = "Friend request rejected." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> Unfriend(Guid friendId)
        {
            var success = await _friendshipService.UnfriendAsync(GetCurrentUserId(), friendId);
            if (!success) return NotFound();
            return Ok(new { Message = "Friendship removed." });
        }

        [HttpPost("block/{targetUserId}")]
        public async Task<IActionResult> BlockUser(Guid targetUserId)
        {
            try
            {
                await _friendshipService.BlockUserAsync(GetCurrentUserId(), targetUserId);
                return Ok(new { Message = "User blocked." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpDelete("unblock/{targetUserId}")]
        public async Task<IActionResult> UnblockUser(Guid targetUserId)
        {
            var success = await _friendshipService.UnblockUserAsync(GetCurrentUserId(), targetUserId);
            if (!success) return NotFound();
            return Ok(new { Message = "User unblocked." });
        }
    }
}