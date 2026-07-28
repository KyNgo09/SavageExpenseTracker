# Mã Tham Khảo: Bảo Mật Trích Xuất User ID Từ JWT Token (Security Code Reference)

Tài liệu này chứa mã nguồn mẫu chuẩn hóa (Refactored Code Reference) để xử lý triệt để lỗ hổng **IDOR (Insecure Direct Object Reference)** bằng cách sử dụng **Extension Method `User.GetUserId()`**.

---

## 1. Extension Method (`ClaimsPrincipalExtensions.cs`)

Tạo file mới: `src/SavageExpenseTracker.WebApi/Extensions/ClaimsPrincipalExtensions.cs`

```csharp
using System;
using System.Security.Claims;

namespace SavageExpenseTracker.WebApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userIdString = user.FindFirstValue(ClaimTypes.Name.Identifier);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Unable to determine current user.");
        }
    }
}
```

---

## 2. `Expense` Module (Controller + Service)

### A. Controller Reference (`src/SavageExpenseTracker.WebApi/Controllers/ExpenseController.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Extensions;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        // GET: api/expenses/me
        // Lấy danh sách chi tiêu của người dùng đang đăng nhập
        [HttpGet("me")]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetMyExpenses()
        {
            var expenses = await _expenseService.GetUserExpensesAsync(User.GetUserId());
            return Ok(expenses);
        }

        // GET: api/expenses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetById(long id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new { Message = "Expense Not Found" });
            }
            return Ok(expense);
        }

        // POST: api/expenses
        // Gán cứng UserId từ Token, loại bỏ nguy cơ mạo danh UserId của người khác trong DTO
        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseDto createExpenseDto)
        {
            try
            {
                createExpenseDto.UserId = User.GetUserId();
                var createdExpense = await _expenseService.CreateExpenseAsync(createExpenseDto);
                return CreatedAtAction(nameof(GetById), new { id = createdExpense.Id }, createdExpense);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // PUT: api/expenses/{id}
        // Truyền User.GetUserId() để Service xác minh quyền sở hữu trước khi cập nhật
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, UpdateExpenseDto updateExpenseDto)
        {
            try
            {
                var result = await _expenseService.UpdateExpenseAsync(id, User.GetUserId(), updateExpenseDto);
                if (!result)
                {
                    return NotFound(new { Message = "Expense Not Found" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/expenses/{id}
        // Truyền User.GetUserId() để Service xác minh quyền sở hữu trước khi xóa
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _expenseService.DeleteExpenseAsync(id, User.GetUserId());
                if (!result)
                {
                    return NotFound(new { Message = $"Can't find Expense with Id: {id}" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
```

### B. Interface Reference (`src/SavageExpenseTracker.Application/Interfaces/IExpenseService.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Expense;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseDto>> GetUserExpensesAsync(Guid userId);
        Task<ExpenseDto?> GetExpenseByIdAsync(long id);
        Task<ExpenseDto> CreateExpenseAsync(CreateExpenseDto createExpenseDto);
        Task<bool> UpdateExpenseAsync(long id, Guid currentUserId, UpdateExpenseDto updateExpenseDto);
        Task<bool> DeleteExpenseAsync(long id, Guid currentUserId);
    }
}
```

### C. Service Implementation Reference (`src/SavageExpenseTracker.Application/Services/ExpenseService.cs`)

```csharp
public async Task<bool> UpdateExpenseAsync(long id, Guid currentUserId, UpdateExpenseDto updateExpenseDto)
{
    var expense = await _expenseRepository.GetByIdAsync(id);
    if (expense == null) return false;

    // Kiểm tra xem Expense có thuộc về User đang đăng nhập hay không
    if (expense.UserId != currentUserId)
    {
        throw new InvalidOperationException("You do not have permission to update this expense.");
    }
    
    var timeWork = expense.AppliedHourlyRate > 0 ? updateExpenseDto.Amount / expense.AppliedHourlyRate : 0;
    expense.Description = updateExpenseDto.Description;
    expense.Amount = updateExpenseDto.Amount;
    expense.TimeWork = timeWork;
    expense.CategoryId = updateExpenseDto.CategoryId;
    
    await _expenseRepository.UpdateAsync(expense);
    return true;
}

public async Task<bool> DeleteExpenseAsync(long id, Guid currentUserId)
{
    var expense = await _expenseRepository.GetByIdAsync(id);
    if (expense == null) return false;

    // Kiểm tra xem Expense có thuộc về User đang đăng nhập hay không
    if (expense.UserId != currentUserId)
    {
        throw new InvalidOperationException("You do not have permission to delete this expense.");
    }

    await _expenseRepository.DeleteAsync(id);
    return true;
}
```

---

## 3. `ChallengeController` Reference (`src/SavageExpenseTracker.WebApi/Controllers/ChallengeController.cs`)

Loại bỏ `[FromBody] Guid userId` ở hai endpoint `join` và `leave`, tự động lấy ID từ Token via `User.GetUserId()`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.WebApi.Extensions;

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

        // POST: api/challenges/{id}/join
        // Sử dụng User.GetUserId() thay vì nhận userId từ Request Body
        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(long id)
        {
            try
            {
                var result = await _challengeService.JoinChallengeAsync(id, User.GetUserId());
                if (!result) return NotFound(new { Message = "Challenge not found." });
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        
        // POST: api/challenges/{id}/leave
        // Sử dụng User.GetUserId() thay vì nhận userId từ Request Body
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> Leave(long id)
        {
            try
            {
                var result = await _challengeService.LeaveChallengeAsync(id, User.GetUserId());
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
```

---

## 4. `UsersController` Reference (`src/SavageExpenseTracker.WebApi/Controllers/UserController.cs`)

Bổ sung API `/me` và kiểm tra quyền chính chủ bằng `User.GetUserId()`:

```csharp
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // PUT: api/users/me
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateUserDto updateUserDto)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(User.GetUserId(), updateUserDto);
                if (!result)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, UpdateUserDto updateUserDto)
        {
            // Kiểm tra chỉ chính chủ hoặc Admin mới được phép cập nhật
            if (id != User.GetUserId() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

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

        // POST: api/users/change-password/{id}
        [HttpPost("change-password/{id}")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            // Kiểm tra chỉ chính chủ mới được phép đổi mật khẩu
            if (id != User.GetUserId())
            {
                return Forbid();
            }

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
    }
}
```

---

## 5. `FriendshipController` Reference (`src/SavageExpenseTracker.WebApi/Controllers/FriendshipsController.cs`)

Thay thế phương thức private `GetCurrentUserId()` bằng `User.GetUserId()`:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Extensions;

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

        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends()
        {
            var friends = await _friendshipService.GetFriendsListAsync(User.GetUserId());
            return Ok(friends);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _friendshipService.GetPendingRequestsAsync(User.GetUserId());
            return Ok(requests);
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendRequest(Guid targetUserId)
        {
            try
            {
                await _friendshipService.SendRequestAsync(User.GetUserId(), targetUserId);
                return Ok(new { Message = "Friend request sent." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("accept/{id}")]
        public async Task<IActionResult> AcceptRequest(long id)
        {
            try
            {
                var success = await _friendshipService.AcceptRequestAsync(User.GetUserId(), id);
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
                var success = await _friendshipService.RejectRequestAsync(User.GetUserId(), id);
                if (!success) return NotFound();
                return Ok(new { Message = "Friend request rejected." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
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
            try
            {
                await _friendshipService.BlockUserAsync(User.GetUserId(), targetUserId);
                return Ok(new { Message = "User blocked." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
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
```

---

## 6. Summary Tóm Tắt

1. **Thêm file Extension**: `ClaimsPrincipalExtensions.cs` trong `WebApi/Extensions`.
2. **Trong Controller**: Sử dụng `using SavageExpenseTracker.WebApi.Extensions;` và gọi `User.GetUserId()`.
3. **Trong Service**: `UpdateExpenseAsync` và `DeleteExpenseAsync` nhận `currentUserId` để check `expense.UserId != currentUserId`.
