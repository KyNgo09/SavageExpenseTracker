using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public UsersController(IUserService UserService)
        {
            _userService = UserService;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUserAsync();
            return Ok(users);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
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
            var user = await _userService.AuthenticateAsync(loginDto);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid Email or Password"});
            }
            return Ok(user);
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