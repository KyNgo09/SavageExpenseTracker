using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Controllers;
using Xunit;

namespace SavageExpenseTracker.WebApi.Tests
{
    public class ChallengeControllerTests
    {
        private readonly Mock<IChallengeService> _challengeServiceMock;
        private readonly ChallengesController _controller;
        private readonly Guid _currentUserId;

        public ChallengeControllerTests()
        {
            _challengeServiceMock = new Mock<IChallengeService>();
            _controller = new ChallengesController(_challengeServiceMock.Object);
            _currentUserId = Guid.NewGuid();

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString()) };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithData()
        {
            var pagedResult = new PagedResultDto<ChallengeDto>
            {
                Items = new List<ChallengeDto> { new ChallengeDto { Id = 1 } },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };
            _challengeServiceMock.Setup(s => s.GetAllChallengesAsync(1, 10)).ReturnsAsync(pagedResult);
            
            var result = await _controller.GetAll(1, 10);
            
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(pagedResult);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenNull()
        {
            _challengeServiceMock.Setup(s => s.GetChallengeByIdAsync(1)).ReturnsAsync((ChallengeDto)null!);
            var result = await _controller.GetById(1);
            
            if (result.Result is NotFoundObjectResult notFoundObj)
            {
                notFoundObj.Value.Should().BeEquivalentTo(new { Message = "Challenge not found." });
            }
            else
            {
                result.Result.Should().BeOfType<NotFoundResult>();
            }
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenExists()
        {
            var dto = new ChallengeDto { Id = 1 };
            _challengeServiceMock.Setup(s => s.GetChallengeByIdAsync(1)).ReturnsAsync(dto);
            var result = await _controller.GetById(1);
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task Create_ShouldReturnOk()
        {
            var createDto = new CreateChallengeDto { Name = "C1" };
            var dto = new ChallengeDto { Id = 1, Name = "C1" };
            _challengeServiceMock.Setup(s => s.CreateChallengeAsync(createDto)).ReturnsAsync(dto);

            var result = await _controller.Create(createDto);
            
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_OnException()
        {
            var createDto = new CreateChallengeDto { Name = "C1" };
            _challengeServiceMock.Setup(s => s.CreateChallengeAsync(createDto)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.Create(createDto);

            var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task Join_ShouldReturnNotFound_WhenFalse()
        {
            _challengeServiceMock.Setup(s => s.JoinChallengeAsync(1, _currentUserId)).ReturnsAsync(false);
            
            var result = await _controller.Join(1);
            
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { Message = "Challenge not found." });
        }

        [Fact]
        public async Task Join_ShouldReturnOk_WhenTrue()
        {
            _challengeServiceMock.Setup(s => s.JoinChallengeAsync(1, _currentUserId)).ReturnsAsync(true);
            var result = await _controller.Join(1);
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Join_ShouldReturnBadRequest_OnException()
        {
            _challengeServiceMock.Setup(s => s.JoinChallengeAsync(1, _currentUserId)).ThrowsAsync(new InvalidOperationException("Error"));
            var result = await _controller.Join(1);
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task Leave_ShouldReturnNotFound_WhenFalse()
        {
            _challengeServiceMock.Setup(s => s.LeaveChallengeAsync(1, _currentUserId)).ReturnsAsync(false);
            var result = await _controller.Leave(1);
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Leave_ShouldReturnOk_WhenTrue()
        {
            _challengeServiceMock.Setup(s => s.LeaveChallengeAsync(1, _currentUserId)).ReturnsAsync(true);
            var result = await _controller.Leave(1);
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Leave_ShouldReturnBadRequest_OnException()
        {
            _challengeServiceMock.Setup(s => s.LeaveChallengeAsync(1, _currentUserId)).ThrowsAsync(new InvalidOperationException("Error"));
            var result = await _controller.Leave(1);
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Error" });
        }

        [Fact]
        public async Task GetLeaderboard_ShouldReturnOk()
        {
            var board = new List<LeaderboardItemDto> { new LeaderboardItemDto { UserId = Guid.NewGuid() } };
            _challengeServiceMock.Setup(s => s.GetLeaderboardAsync(1)).ReturnsAsync(board);
            var result = await _controller.GetLeaderboard(1);
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(board);
        }
    }
}
