# Triển khai JWT Token, Phân Quyền & Refresh Token - SavageExpenseTracker

Việc chỉ dùng JWT Access Token thông thường sẽ có rủi ro nếu token bị lộ (bởi vì nó có hạn sử dụng quá dài). Giải pháp chuẩn bảo mật (Best Practice) là:
1. **Access Token**: Cực ngắn (ví dụ: 15 phút - 1 tiếng). Dùng để gọi API.
2. **Refresh Token**: Dài hơn (ví dụ: 7 ngày). Được lưu trong DB. Khi Access Token hết hạn, client gửi Refresh Token này lên để xin một cặp Access Token + Refresh Token mới.

Dưới đây là các bước để bạn triển khai:

## 1. Cập nhật Entity `User` (Domain Layer)
Thêm các trường `Role` và thông tin lưu trữ `RefreshToken` vào bảng `User`.
Mở `src/SavageExpenseTracker.Domain/Entities/User.cs`:
```csharp
namespace SavageExpenseTracker.Domain.Entities
{
    public class User
    {
        // Các thuộc tính hiện có...
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        
        // 1. Phân quyền
        public string Role { get; set; } = "User"; 

        // 2. Refresh Token
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
```
*(Đừng quên chạy Migration: `dotnet ef migrations add AddJwtAndRoleFields` và `dotnet ef database update`)*.

---

## 2. Tạo Token Service

### 2.1. Dto & Interface
Tạo class `AuthenticationResult` ở Application để trả về kết quả 2 token:
```csharp
// File: src/SavageExpenseTracker.Application/Dtos/User/TokenResponseDto.cs
namespace SavageExpenseTracker.Application.Dtos.User
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
```

Cập nhật Interface `ITokenService.cs`:
```csharp
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Application.Dtos.User;
using System.Security.Claims;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
```

### 2.2. Implementation `TokenService.cs`
Cập nhật file `src/SavageExpenseTracker.Infrastructure/Services/TokenService.cs`:

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
            var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "FallbackSecretKey12345678901234567890";
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "SavageExpenseTrackerApi";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "SavageExpenseTrackerClient";

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
            var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "FallbackSecretKey12345678901234567890";
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // Không cần validate ở bước rút trích
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
                ValidateLifetime = false // Mấu chốt: Token ĐÃ HẾT HẠN vẫn cho phép giải mã để lấy thông tin
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
```

---

## 3. Cập nhật `Program.cs`
File này giữ nguyên logic như hướng dẫn phân quyền trước (có `RoleClaimType = ClaimTypes.Role`), tuy nhiên hãy đảm bảo tham số `ClockSkew = TimeSpan.Zero` để Token hết hạn chính xác tới từng giây.

---

## 4. API Login & API Refresh Token
Tạo một Dto mới để nhận Request `TokenApiModel`:
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

Trong `UserController.cs`, bạn cần inject `IUserRepository` (hoặc tạo các hàm lưu database trong `IUserService`). Ví dụ tôi sẽ tương tác thông qua Interface của User:

```csharp
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
// ... (các using khác)

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository; // Inject thêm repo để cập nhật RefreshToken nhanh

        public UserController(IUserService userService, ITokenService tokenService, IUserRepository userRepository)
        {
            _userService = userService;
            _tokenService = tokenService;
            _userRepository = userRepository;
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _userService.AuthenticateAsync(loginDto);
            if (user == null) return Unauthorized(new { Message = "Sai thông tin đăng nhập" });

            // Lấy thông tin user entity từ DB
            var userEntity = await _userRepository.GetByIdAsync(user.Id);
            if(userEntity == null) return BadRequest();

            // 1. Sinh Access Token (15 phút) và Refresh Token
            var accessToken = _tokenService.GenerateAccessToken(userEntity);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // 2. Lưu Refresh Token vào Database với HSD là 7 ngày
            userEntity.RefreshToken = refreshToken;
            userEntity.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(userEntity);

            return Ok(new TokenResponseDto 
            { 
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        // --- ĐÂY LÀ API MỚI ĐỂ GỌI KHI ACCESS TOKEN BỊ LỖI 401 ---
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenApiModel tokenApiModel)
        {
            if (tokenApiModel is null) return BadRequest("Invalid client request");

            string accessToken = tokenApiModel.AccessToken;
            string refreshToken = tokenApiModel.RefreshToken;

            // 1. Giải mã token cũ để lấy ID của User
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (!Guid.TryParse(userIdString, out Guid userId)) 
                return BadRequest("Invalid Token");

            // 2. Truy vấn user từ DB
            var user = await _userRepository.GetByIdAsync(userId);

            // 3. Kiểm tra tính hợp lệ của Refresh Token
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid client request or Refresh Token has expired");
            }

            // 4. Sinh cặp token mới
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // 5. Cập nhật lại vào DB
            user.RefreshToken = newRefreshToken;
            await _userRepository.UpdateAsync(user);

            return Ok(new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
    }
}
```

### Luồng hoạt động Client - Server (Dành cho FE xử lý):
1. User **đăng nhập**, nhận về `AccessToken` (HSD: 15 phút) và `RefreshToken` (HSD: 7 ngày).
2. Khi User gọi API gửi Expense, FE đính kèm `AccessToken` lên Header. API trả về `200 OK`.
3. Khi 15 phút trôi qua, User lại gọi API. Do `AccessToken` hết hạn, hệ thống trả về mã lỗi `401 Unauthorized`.
4. Lúc này, mã FE (ví dụ Axios Interceptor) sẽ ngầm **bắt** lỗi 401 này, và lập tức gửi `AccessToken` (cũ) và `RefreshToken` xuống API `/api/users/refresh-token`.
5. Hệ thống check nếu `RefreshToken` chưa quá 7 ngày, nó sẽ trả về **cặp token mới**.
6. FE tự động cập nhật token mới vào kho (LocalStorage) và *chạy lại cái API gửi Expense bị kẹt ban nãy*.
👉 **Kết quả**: Người dùng không hề bị gián đoạn hay văng ra màn hình đăng nhập, mọi thứ diễn ra ngầm sau lưng họ. Kẻ gian dù có trộm được Access Token cũng chỉ xài được 15 phút.
