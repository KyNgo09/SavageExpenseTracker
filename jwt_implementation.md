# JWT Implementation — SavageExpenseTracker

Phân tích codebase hiện tại cho thấy:
- `User.cs` → ✅ đã có `Role`, `RefreshToken`, `RefreshTokenExpiryDate`  
- `ITokenService.cs` → ✅ đã có 3 methods đúng theo spec  
- `TokenService.cs` → ✅ đã implement đủ, cần dùng **fail-fast** thay vì fallback key  
- `TokenResponseDto.cs` → ✅ đã tồn tại  
- `UserController.cs` → ❌ Login chỉ trả `UserDto`, chưa trả token; thiếu `refresh-token` endpoint  
- `Program.cs` → ❌ Chưa đăng ký JWT Auth, chưa đăng ký `ITokenService`  

> **Lưu ý tên field**: `User.cs` dùng `RefreshTokenExpiryDate` (khác với tài liệu ghi `RefreshTokenExpiryTime`). Code dưới đây dùng đúng tên field hiện có trong codebase.

> [!CAUTION]
> **Không dùng fallback/hardcoded key**. Nếu env var bị thiếu ở production, app phải **crash ngay lập tức** (fail-fast) thay vì chạy với key đã biết trước. Kẻ tấn công có thể forge JWT token hợp lệ nếu biết secret key.

---

## 1. [NEW] `TokenApiModel.cs` — Nhận request Refresh Token

**File:** `src/SavageExpenseTracker.Application/Dtos/User/TokenApiModel.cs`

```csharp
namespace SavageExpenseTracker.Application.Dtos.User
{
    public class TokenApiModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
```

---

## 2. [MODIFY] `TokenService.cs` — Thêm fallback key

**File:** `src/SavageExpenseTracker.Infrastructure/Services/TokenService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        public string GenerateAccessToken(User user)
        {
            // FAIL FAST: Nếu thiếu config → crash ngay, không dùng fallback
            var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? throw new InvalidOperationException("JWT_ISSUER environment variable is not set.");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is not set.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                // ACCESS TOKEN NGẮN HẠN: 15 PHÚT
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            // FAIL FAST: Nếu thiếu config → crash ngay, không dùng fallback
            var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,   // Không cần validate ở bước rút trích
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
                ValidateLifetime = false    // Mấu chốt: Token ĐÃ HẾT HẠN vẫn cho phép giải mã
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}
```

---

## 3. [MODIFY] `Program.cs` — Đăng ký JWT Auth + `ITokenService`

**File:** `src/SavageExpenseTracker.WebApi/Program.cs`

Thêm `using` ở đầu file:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using SavageExpenseTracker.Infrastructure.Services;
```

Thêm khối đăng ký JWT **trước** `var app = builder.Build();` (sau phần "Register service"):

```csharp
// Register TokenService
builder.Services.AddScoped<ITokenService, TokenService>();

// FAIL FAST: Validate JWT config tại startup — app crash sớm thay vì bị exploit
// Nếu thiếu bất kỳ env var nào, exception sẽ được throw ngay khi khởi động
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? throw new InvalidOperationException("JWT_ISSUER environment variable is not set.");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is not set.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // Token hết hạn chính xác tới từng giây
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = ClaimTypes.Role // Để [Authorize(Roles = "Admin")] hoạt động đúng
    };
});

builder.Services.AddAuthorization();
```

Thêm middleware **sau** `app.UseHttpsRedirection()` và **trước** `app.MapControllers()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

---

## 4. [MODIFY] `UserController.cs` — Cập nhật Login + thêm RefreshToken endpoint

**File:** `src/SavageExpenseTracker.WebApi/Controllers/UserController.cs`

```csharp
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
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var userDto = await _userService.AuthenticateAsync(loginDto);
            if (userDto == null)
            {
                return Unauthorized(new { Message = "Invalid Email or Password" });
            }

            // Lấy User entity từ DB để sinh token và lưu refresh token
            var userEntity = await _userRepository.GetByIdAsync(userDto.Id);
            if (userEntity == null) return BadRequest(new { Message = "User not found" });

            // 1. Sinh Access Token (15 phút) và Refresh Token
            var accessToken = _tokenService.GenerateAccessToken(userEntity);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // 2. Lưu Refresh Token vào Database với HSD là 7 ngày
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

            // 1. Giải mã token cũ để lấy ID của User
            ClaimsPrincipal principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            }
            catch
            {
                return BadRequest(new { Message = "Invalid Access Token" });
            }

            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdString, out Guid userId))
                return BadRequest(new { Message = "Invalid Token" });

            // 2. Truy vấn user từ DB
            var user = await _userRepository.GetByIdAsync(userId);

            // 3. Kiểm tra tính hợp lệ của Refresh Token
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryDate <= DateTime.UtcNow)
            {
                return BadRequest(new { Message = "Invalid client request or Refresh Token has expired" });
            }

            // 4. Sinh cặp token mới
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // 5. Cập nhật lại Refresh Token vào DB (Refresh Token Rotation)
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserDto>>> Search([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "Email can't be empty!" });
            }
            var users = await _userService.SearchUserByEmailAsync(email);
            return Ok(users);
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Authorize]
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
```

---

## 5. Migration (chạy sau khi gõ code xong)

> [!NOTE]
> Entity `User.cs` đã có `Role`, `RefreshToken`, `RefreshTokenExpiryDate` nhưng migration có thể chưa được tạo. Kiểm tra migration cuối cùng, nếu chưa có thì chạy:

```bash
dotnet ef migrations add AddJwtAndRoleFields --project src/SavageExpenseTracker.Infrastructure --startup-project src/SavageExpenseTracker.WebApi
dotnet ef database update --project src/SavageExpenseTracker.Infrastructure --startup-project src/SavageExpenseTracker.WebApi
```

---

## Tóm tắt thứ tự gõ tay

| # | File | Thao tác |
|---|------|----------|
| 1 | `Dtos/User/TokenApiModel.cs` | Tạo mới |
| 2 | `Infrastructure/Services/TokenService.cs` | Sửa: thêm fallback key |
| 3 | `WebApi/Program.cs` | Sửa: thêm usings + đăng ký JWT + middleware |
| 4 | `WebApi/Controllers/UserController.cs` | Sửa: inject thêm deps + cập nhật Login + thêm RefreshToken |
| 5 | Migration | Chạy lệnh nếu cần |
