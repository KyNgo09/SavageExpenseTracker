# Tài liệu tham khảo mã nguồn SavageExpenseTracker - Mô-đun User (Cập nhật)

Tài liệu này chứa mã nguồn mẫu đầy đủ của thực thể **User** đã được cập nhật theo đúng các phương thức mới trong hợp đồng [IUserRepository.cs](file:///d:/SavageExpenseTracker/src/SavageExpenseTracker.Application/Interfaces/IUserRepository.cs) và [IUserService.cs](file:///d:/SavageExpenseTracker/src/SavageExpenseTracker.Application/Interfaces/IUserService.cs) của bạn.

---

## 1. Lớp Domain (`SavageExpenseTracker.Domain`)

### File: `Entities/User.cs`
*(Bổ sung thêm thuộc tính `Username`)*
```csharp
using System;

namespace SavageExpenseTracker.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

---

## 2. Lớp Application (`SavageExpenseTracker.Application`)

### File: `Dtos/User/UserDto.cs`
*(DTO trả về thông tin User - thêm Username)*
```csharp
using System;

namespace SavageExpenseTracker.Application.Dtos.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
```

### File: `Dtos/User/CreateUserDto.cs`
*(DTO nhận yêu cầu Đăng ký - thêm Username)*
```csharp
namespace SavageExpenseTracker.Application.Dtos.User
{
    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; } = 20000;
    }
}
```

### File: `Dtos/User/UpdateUserDto.cs`
*(DTO nhận yêu cầu Cập nhật - thêm Username)*
```csharp
namespace SavageExpenseTracker.Application.Dtos.User
{
    public class UpdateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}
```

### File: `Dtos/User/LoginDto.cs`
```csharp
namespace SavageExpenseTracker.Application.Dtos.User
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
```

### File: `Dtos/User/ChangePasswordDto.cs`
```csharp
namespace SavageExpenseTracker.Application.Dtos.User
{
    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
```

### File: `Interfaces/IUserRepository.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IUserRepository
    {
        // Get Methods
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUserNameAsync(string username);
        
        // Create, Update, Delete Methods
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);

        // Exist Methods
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UserNameExistsAsync(string username);
    }
}
```

### File: `Interfaces/IUserService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.User;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IUserService
    {
        // Get Methods
        Task<IEnumerable<UserDto>> GetAllUserAsync();
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto?> GetUserByUserNameAsync(string username);

        // Create, Update, Delete Methods
        Task<UserDto> RegisterUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(Guid id);

        // Exist Methods
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UserNameExistsAsync(string username);

        // Authentication Methods
        Task<UserDto?> AuthenticateAsync(LoginDto loginDto);
        Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changePasswordDto);
        
        // Search Methods
        Task<IEnumerable<UserDto>> SearchUserByEmailAsync(string query);
    }
}
```

### File: `Services/UserService.cs`
*(Triển khai đầy đủ tất cả nghiệp vụ bao gồm: Đăng nhập, Đổi mật khẩu, Tìm kiếm, Đăng ký và CRUD)*
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserDto>> GetAllUserAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => MapToDto(u));
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto?> GetUserByUserNameAsync(string username)
        {
            var user = await _userRepository.GetByUserNameAsync(username);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto> RegisterUserAsync(CreateUserDto createUserDto)
        {
            // 1. Kiểm tra trùng lặp email và username dưới DB bằng các hàm Exists tối ưu
            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
            {
                throw new InvalidOperationException("Email này đã được sử dụng.");
            }

            if (await _userRepository.UserNameExistsAsync(createUserDto.Username))
            {
                throw new InvalidOperationException("Username này đã tồn tại.");
            }

            // 2. Hash mật khẩu thô
            var passwordHash = HashPassword(createUserDto.Password);

            // 3. Tạo Entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = createUserDto.Email,
                Username = createUserDto.Username,
                PasswordHash = passwordHash,
                HourlyRate = createUserDto.HourlyRate,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return MapToDto(user);
        }

        public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            // Kiểm tra trùng email nếu cập nhật email mới
            if (user.Email != updateUserDto.Email && await _userRepository.EmailExistsAsync(updateUserDto.Email))
            {
                throw new InvalidOperationException("Email mới đã được sử dụng bởi người dùng khác.");
            }

            // Kiểm tra trùng username nếu cập nhật username mới
            if (user.Username != updateUserDto.Username && await _userRepository.UserNameExistsAsync(updateUserDto.Username))
            {
                throw new InvalidOperationException("Username mới đã tồn tại.");
            }

            user.Email = updateUserDto.Email;
            user.Username = updateUserDto.Username;
            user.HourlyRate = updateUserDto.HourlyRate;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userRepository.EmailExistsAsync(email);
        }

        public async Task<bool> UserNameExistsAsync(string username)
        {
            return await _userRepository.UserNameExistsAsync(username);
        }

        // Logic Đăng Nhập (So khớp Hash)
        public async Task<UserDto?> AuthenticateAsync(LoginDto loginDto)
        {
            // Tìm user qua Email
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            
            // Nếu không tìm thấy bằng email, thử tìm bằng Username
            if (user == null)
            {
                user = await _userRepository.GetByUserNameAsync(loginDto.Email); // Nhập username vào ô Email
            }

            if (user == null) return null;

            // Mã hóa mật khẩu đăng nhập gửi lên và so khớp
            var hashInput = HashPassword(loginDto.Password);
            if (user.PasswordHash != hashInput) return null;

            return MapToDto(user);
        }

        // Logic Đổi mật khẩu
        public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changePasswordDto)
        {
            // 1. Kiểm tra xác nhận mật khẩu mới khớp nhau
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
            {
                throw new InvalidOperationException("Mật khẩu mới và mật khẩu xác nhận không khớp.");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            // 2. Xác thực mật khẩu cũ
            var oldHash = HashPassword(changePasswordDto.OldPassword);
            if (user.PasswordHash != oldHash)
            {
                throw new InvalidOperationException("Mật khẩu cũ không chính xác.");
            }

            // 3. Hash mật khẩu mới và lưu
            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        // Tìm kiếm người dùng theo Email
        public async Task<IEnumerable<UserDto>> SearchUserByEmailAsync(string query)
        {
            var allUsers = await _userRepository.GetAllAsync();
            var matchedUsers = allUsers.Where(u => u.Email.Contains(query, StringComparison.OrdinalIgnoreCase));
            return matchedUsers.Select(u => MapToDto(u));
        }

        // Helper: Ánh xạ từ Entity sang DTO để tránh lặp code (DRY Principle)
        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                HourlyRate = user.HourlyRate,
                CreatedAt = user.CreatedAt
            };
        }

        // Helper: Mã hóa SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
```

---

## 3. Lớp Infrastructure (`SavageExpenseTracker.Infrastructure`)

### File: `Repositories/UserRepository.cs`
*(Cập nhật thêm các hàm Exists và GetByUserName)*
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;

namespace SavageExpenseTracker.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public UserRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUserNameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        // Tối ưu hiệu năng: dùng AnyAsync để DB sinh câu lệnh SELECT EXISTS gọn nhẹ
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> UserNameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }
    }
}
```

---

## 4. Lớp WebApi (`SavageExpenseTracker.WebApi`)

### File: `Controllers/UsersController.cs`
*(Cập nhật thêm các endpoint: Đăng nhập, Đổi mật khẩu, Tìm kiếm)*
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Dtos.User;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllUserAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = $"Không tìm thấy người dùng với ID: {id}" });
            }
            return Ok(user);
        }

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

        // API Đăng nhập
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userService.AuthenticateAsync(loginDto);
            if (user == null)
            {
                return Unauthorized(new { Message = "Email/Username hoặc mật khẩu không chính xác." });
            }
            return Ok(user);
        }

        // API Đổi mật khẩu
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid id, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
                if (!result)
                {
                    return NotFound(new { Message = $"Không tìm thấy người dùng với ID: {id}" });
                }
                return Ok(new { Message = "Thay đổi mật khẩu thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // API Tìm kiếm bạn bè qua email
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDto>>> Search([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "Từ khóa tìm kiếm không được để trống." });
            }
            var users = await _userService.SearchUserByEmailAsync(email);
            return Ok(users);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateUserDto updateUserDto)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, updateUserDto);
                if (!result)
                {
                    return NotFound(new { Message = $"Không tìm thấy người dùng với ID: {id}" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { Message = $"Không tìm thấy người dùng với ID: {id}" });
            }
            return NoContent();
        }
    }
}
```
 File Repository thực thi: UserRepository.cs
Vị trí tạo: Tạo thư mục Repositories/ trong dự án SavageExpenseTracker.Infrastructure và thêm file UserRepository.cs.
Nhiệm vụ: Thực hiện việc truy vấn (Select, Insert, Update, Delete) trực tiếp vào Database thông qua DbContext đã tạo ở trên.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;

namespace SavageExpenseTracker.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public UserRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUserNameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        // Tối ưu hiệu năng kiểm tra tồn tại Email
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        // Tối ưu hiệu năng kiểm tra tồn tại Username
        public async Task<bool> UserNameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }
    }
}
```