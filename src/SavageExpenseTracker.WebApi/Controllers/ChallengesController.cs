using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Extensions;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/challenges")]
    [Authorize]
    public class ChallengesController : ControllerBase
    {
        private readonly IChallengeService _challengeService;

        public ChallengesController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        // GET: api/challenges?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ChallengeDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var pagedChallenges = await _challengeService.GetAllChallengesAsync(pageNumber, pageSize);
            return Ok(pagedChallenges);
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
            var result = await _challengeService.CreateChallengeAsync(dto);
            return Ok(result);
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(long id)
        {
            var result = await _challengeService.JoinChallengeAsync(id, User.GetUserId());
            if (!result) return NotFound(new { Message = "Challenge not found." });
            return Ok();
        }
        
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> Leave(long id)
        {
            var result = await _challengeService.LeaveChallengeAsync(id, User.GetUserId());
            if (!result) return NotFound();
            return Ok();
        }

        [HttpGet("{id}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(long id)
        {
            var leaderboard = await _challengeService.GetLeaderboardAsync(id);
            return Ok(leaderboard);
        }
    }
}
