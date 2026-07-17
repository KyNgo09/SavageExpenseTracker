using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Dtos.User;

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

        // GET: api/users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUserAsync();
            return Ok(users);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = $"Can't find user with ID: {id}" });
            }
            return Ok(user);
        }

        // POST: api/users/register
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(CreateUserDto createUserDto)
        {
            try
            {
                var createdUser = await _userService.RegisterUserAsync(createUserDto);
                return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
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

            // Generate access token and refresh token
            var userEntity = await _userRepository.GetByIdAsync(userDto.Id);
            if (userEntity == null) return BadRequest(new { Message = "User not found" });

            var accessToken = _tokenService.GenerateAccessToken(userEntity);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save the refresh token to the database
            userEntity.RefreshToken = refreshToken;
            userEntity.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7); // Set refresh token expiry time
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
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
                if (!result)
                {
                    return NotFound(new { Message = $"Can't change password for user with ID: {id}" });
                }
                return Ok(new { Message = "Change password successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET: api/users/search?email={email}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDto>>> Search([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "Email can't be empty!"});
            }
            var users = await _userService.SearchUserByEmailAsync(email);
            return Ok(users);
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateUserDto updateUserDto)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, updateUserDto);
                if (!result)
                {
                    return NotFound(new { Message = $"Can't update user with ID: {id}" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
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