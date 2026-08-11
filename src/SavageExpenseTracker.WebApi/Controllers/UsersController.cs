using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Constants;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Constants;
using SavageExpenseTracker.WebApi.Extensions;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UsersController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // GET: api/users?pageNumber=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
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
        public async Task<ActionResult<TokenResponseDto>> Login(LoginDto loginDto)
        {
            var tokenResponse = await _authService.LoginAsync(loginDto);
            if (tokenResponse == null)
            {
                return Unauthorized(new { Message = "Invalid Email or Password" });
            }

            return Ok(tokenResponse);
        }

        // POST: api/users/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenApiModel tokenApiModel)
        {
            if (tokenApiModel is null) return BadRequest("Invalid client request");

            var tokenResponse = await _authService.RefreshTokenAsync(tokenApiModel);
            if (tokenResponse == null)
            {
                return BadRequest(new { Message = "Invalid client request or Refresh Token has expired" });
            }

            return Ok(tokenResponse);
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

            var result = await _authService.ChangePasswordAsync(id, changePasswordDto);
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
                return BadRequest(new { Message = "Email can't be empty!" });
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
            if (id != User.GetUserId() && !User.IsInRole(UserRoles.Admin))
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
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { Message = $"Can't delete user with ID: {id}" });
            }
            return NoContent();
        }

        // POST: api/users/me/avatar
        [HttpPost("me/avatar")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Please select an image file for avatar!" });

            using var stream = file.OpenReadStream();
            var updatedUser = await _userService.UploadAvatarAsync(User.GetUserId(), stream, file.FileName, file.ContentType, file.Length);
            if (updatedUser == null)
                return NotFound(new { Message = "User not found!" });

            return Ok(updatedUser);
        }
    }
}
