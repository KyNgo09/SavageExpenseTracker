using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Dtos.Challenge;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/challenges")]
    [Authorize]
    public class ChallengeController : ControllerBase
    {
        private readonly IChallengeService _challengeService;

        public ChallengeController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetAll()
        {
            var result = await _challengeService.GetAllChallengesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChallengeDto>> GetById(long id)
        {
            var result = await _challengeService.GetChallengeByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ChallengeDto>> Create(CreateChallengeDto dto)
        {
            try
            {
                var result = await _challengeService.CreateChallengeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(long id, [FromBody] Guid userId)
        {
            try
            {
                var result = await _challengeService.JoinChallengeAsync(id, userId);
                if (!result) return NotFound(new { Message = "Phòng không tồn tại." });
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> Leave(long id, [FromBody] Guid userId)
        {
            try
            {
                var result = await _challengeService.LeaveChallengeAsync(id, userId);
                if (!result) return NotFound();
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{id}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(long id)
        {
            var leaderboard = await _challengeService.GetLeaderboardAsync(id);
            return Ok(leaderboard);
        }
    }
}
