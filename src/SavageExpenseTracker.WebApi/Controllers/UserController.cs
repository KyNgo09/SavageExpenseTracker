using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.WebApi.Extensions;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;

        public UsersController(IUserService userService, ITokenService tokenService, IUserRepository userRepository)
        {
            _userService = userService;
            _tokenService = tokenService;
            _userRepository = userRepository;
        }

        // GET: api/users?pageNumber=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResultDto<UserDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var pagedUsers = await _userService.GetAllUsersAsync(pageNumber, pageSize);
            return Ok(pagedUsers);
        }

        // GET: api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetMe()
        {
            var user = await _userService.GetUserByIdAsync(User.GetUserId());
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }
            return Ok(user);
        }

        // POST: api/users/register
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(CreateUserDto createUserDto)
        {
            var createdUser = await _userService.RegisterUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetMe), new { id = createdUser.Id }, createdUser);
        }

        // POST: api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var userDto = await _userService.AuthenticateAsync(loginDto);
            if (userDto == null)
            {
                return Unauthorized(new { Message = "Invalid Email or Password"});
            }

            var userEntity = await _userRepository.GetByIdAsync(userDto.Id);
            if (userEntity == null) return BadRequest(new { Message = "User not found" });

            var accessToken = _tokenService.GenerateAccessToken(userEntity);
            var refreshToken = _tokenService.GenerateRefreshToken();

            userEntity.RefreshToken = refreshToken;
            userEntity.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(userEntity);

            return Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        // POST: api/users/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenApiModel tokenApiModel)
        {
            if (tokenApiModel is null) return BadRequest("Invalid client request");

            string accessToken = tokenApiModel.AccessToken;
            string refreshToken = tokenApiModel.RefreshToken;

            ClaimsPrincipal principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            }
            catch
            {
                return BadRequest(new { Message = "Invalid access token or refresh" });
            }

            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdString, out Guid userId))
                return BadRequest(new { Message = "Invalid Token "});

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryDate <= DateTime.UtcNow)
            {
                return BadRequest(new { Message = "Invalid client request or Refresh Token has expired" });
            }

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            return Ok(new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
        
        // POST: api/users/change-password/{id}
        [HttpPost("change-password/{id}")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            if (id != User.GetUserId())
            {
                return Forbid();
            }

            var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
            if (!result)
            {
                return NotFound(new { Message = $"Can't change password for user with ID: {id}" });
            }
            return Ok(new { Message = "Change password successfully" });
        }

        // GET: api/users/search?email={email}&pageNumber=1&pageSize=10
        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<PagedResultDto<UserDto>>> Search([FromQuery] string email, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "Email can't be empty!"});
            }
            var pagedUsers = await _userService.SearchUserByEmailAsync(email, pageNumber, pageSize);
            return Ok(pagedUsers);
        }

        // PUT: api/users/me
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateUserDto updateUserDto)
        {
            var result = await _userService.UpdateUserAsync(User.GetUserId(), updateUserDto);
            if (!result)
            {
                return NotFound(new { Message = $"Can't update user with ID: {User.GetUserId()}" });
            }
            return NoContent();
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, UpdateUserDto updateUserDto)
        {
            if (id != User.GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!result)
            {
                return NotFound(new { Message = $"Can't update user with ID: {id}" });
            }
            return NoContent();
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { Message = $"Can't delete user with ID: {id}" });
            }
            return NoContent();
        }
    }
}