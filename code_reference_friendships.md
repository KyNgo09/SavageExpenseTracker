# Code Reference: Tính năng Kết bạn (Friendships)

Dựa trên phân tích từ `friendship_workflow.md` và tuân thủ chặt chẽ Clean Architecture cũng như phong cách code (Convention) hiện tại của dự án `SavageExpenseTracker`, dưới đây là toàn bộ mã nguồn tham khảo được sắp xếp theo ĐÚNG THỨ TỰ ƯU TIÊN PHÁT TRIỂN (từ Lõi ra Giao diện) để bạn có thể gõ theo.

---

## BƯỚC 1: Domain Layer (`src/SavageExpenseTracker.Domain/Entities`)

### `Friendship.cs`
Theo thiết kế 1 dòng duy nhất, chúng ta dùng một `Id` tự tăng làm khóa chính để dễ dàng quản lý việc query.

```csharp
using System;

namespace SavageExpenseTracker.Domain.Entities
{
    public class Friendship
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }   // Người thực hiện hành động (gửi lời mời, hoặc chặn)
        public Guid FriendId { get; set; } // Người nhận hành động
        public string Status { get; set; } = "pending"; // pending, accepted, blocked
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public User? Friend { get; set; }
    }
}
```

---

## BƯỚC 2: Application Layer - Hợp đồng & DTOs (`src/SavageExpenseTracker.Application`)

### 2.1 DTOs (`Dtos/Friendship/`)
```csharp
// UserProfileDto.cs (Dùng chung cho trả về list bạn bè)
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// FriendshipRequestDto.cs (Dùng cho GetPendingRequests)
public class FriendshipRequestDto
{
    public long FriendshipId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
```

### 2.2 `Interfaces/IFriendshipRepository.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IFriendshipRepository
    {
        Task<Friendship?> GetByIdAsync(long id);
        Task<Friendship?> GetFriendshipBetweenAsync(Guid userA, Guid userB);
        Task AddAsync(Friendship friendship);
        Task UpdateAsync(Friendship friendship);
        Task DeleteAsync(Friendship friendship);
        
        Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId);
        Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId);
    }
}
```

### 2.3 `Interfaces/IFriendshipService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Friendship;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IFriendshipService
    {
        Task<bool> SendRequestAsync(Guid currentUserId, Guid targetUserId);
        Task<bool> AcceptRequestAsync(Guid currentUserId, long friendshipId);
        Task<bool> RejectRequestAsync(Guid currentUserId, long friendshipId);
        Task<bool> UnfriendAsync(Guid currentUserId, Guid friendId);
        Task<bool> BlockUserAsync(Guid currentUserId, Guid targetUserId);
        Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId);

        Task<IEnumerable<UserProfileDto>> GetFriendsListAsync(Guid currentUserId);
        Task<IEnumerable<FriendshipRequestDto>> GetPendingRequestsAsync(Guid currentUserId);
    }
}
```

---

## BƯỚC 3: Infrastructure Layer - Kết nối Database (`src/SavageExpenseTracker.Infrastructure`)

### 3.1 Cập nhật `Data/SavageExpenseTrackerDbContext.cs`

Thêm `DbSet` vào class `SavageExpenseTrackerDbContext`:
```csharp
public DbSet<Friendship> Friendships => Set<Friendship>();
```

Thêm cấu hình bảng vào `OnModelCreating(ModelBuilder modelBuilder)`:
```csharp
modelBuilder.Entity<Friendship>(entity =>
{
    entity.ToTable("friendships");
    
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
    
    entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
    entity.Property(e => e.FriendId).HasColumnName("friend_id").IsRequired();
    entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

    // Ràng buộc khóa ngoại
    entity.HasOne(e => e.User)
        .WithMany() // User không nhất thiết phải chứa List<Friendship> để tránh loop, query thông qua Repo
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(e => e.Friend)
        .WithMany()
        .HasForeignKey(e => e.FriendId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

*(Lúc này, hãy mở Terminal chạy lệnh `dotnet ef migrations add AddFriendshipTable` và `dotnet ef database update`)*

### 3.2 `Repositories/FriendshipRepository.cs`
Triển khai `IFriendshipRepository`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;
using SavageExpenseTracker.Infrastructure.Data;

namespace SavageExpenseTracker.Infrastructure.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public FriendshipRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<Friendship?> GetByIdAsync(long id)
        {
            return await _context.Friendships.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Friendship?> GetFriendshipBetweenAsync(Guid userA, Guid userB)
        {
            // Tìm record bất kể ai là người gửi
            return await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.UserId == userA && f.FriendId == userB) || 
                    (f.UserId == userB && f.FriendId == userA));
        }

        public async Task AddAsync(Friendship friendship)
        {
            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Friendship friendship)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Friendship friendship)
        {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId)
        {
            return await _context.Friendships
                .Include(f => f.User)
                .Include(f => f.Friend)
                .Where(f => f.Status == "accepted" && (f.UserId == userId || f.FriendId == userId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid userId)
        {
            // Lời mời MÌNH NHẬN ĐƯỢC (friend_id = mình, status = pending)
            return await _context.Friendships
                .Include(f => f.User) // Lấy thông tin người gửi
                .Where(f => f.FriendId == userId && f.Status == "pending")
                .ToListAsync();
        }
    }
}
```

---

## BƯỚC 4: Application Layer - Triển khai Logic Nghiệp vụ (`src/SavageExpenseTracker.Application`)

### 4.1 `Services/FriendshipService.cs`
Triển khai đúng State Machine theo yêu cầu trong `friendship_workflow.md`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Friendship;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;

        public FriendshipService(IFriendshipRepository friendshipRepository)
        {
            _friendshipRepository = friendshipRepository;
        }

        public async Task<bool> SendRequestAsync(Guid currentUserId, Guid targetUserId)
        {
            if (currentUserId == targetUserId) throw new InvalidOperationException("Không thể tự kết bạn với chính mình.");

            var existing = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);
            
            if (existing != null)
            {
                if (existing.Status == "blocked")
                    throw new InvalidOperationException("Không thể kết bạn với người dùng này.");
                if (existing.Status == "accepted")
                    throw new InvalidOperationException("Hai người đã là bạn bè.");
                if (existing.Status == "pending")
                    throw new InvalidOperationException("Lời mời kết bạn đang chờ xử lý.");
            }

            var friendship = new Friendship
            {
                UserId = currentUserId,
                FriendId = targetUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipRepository.AddAsync(friendship);
            return true;
        }

        public async Task<bool> AcceptRequestAsync(Guid currentUserId, long friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
            if (friendship == null) return false;

            // Chỉ người nhận (friend_id) mới được accept
            if (friendship.FriendId != currentUserId || friendship.Status != "pending")
                throw new InvalidOperationException("Không thể chấp nhận lời mời này.");

            friendship.Status = "accepted";
            await _friendshipRepository.UpdateAsync(friendship);
            return true;
        }

        public async Task<bool> RejectRequestAsync(Guid currentUserId, long friendshipId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
            if (friendship == null) return false;

            // Người nhận từ chối HOẶC người gửi rút lại lời mời
            if ((friendship.FriendId == currentUserId || friendship.UserId == currentUserId) && friendship.Status == "pending")
            {
                await _friendshipRepository.DeleteAsync(friendship);
                return true;
            }
            throw new InvalidOperationException("Không thể từ chối lời mời này.");
        }

        public async Task<bool> UnfriendAsync(Guid currentUserId, Guid friendId)
        {
            var friendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, friendId);
            if (friendship == null || friendship.Status != "accepted") return false;

            await _friendshipRepository.DeleteAsync(friendship);
            return true;
        }

        public async Task<bool> BlockUserAsync(Guid currentUserId, Guid targetUserId)
        {
            if (currentUserId == targetUserId) throw new InvalidOperationException("Không thể tự chặn chính mình.");

            var existing = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);
            
            if (existing != null)
            {
                // Xóa hoặc ghi đè record hiện tại
                existing.UserId = currentUserId; // Người chặn luôn nằm ở UserId
                existing.FriendId = targetUserId;
                existing.Status = "blocked";
                await _friendshipRepository.UpdateAsync(existing);
            }
            else
            {
                var friendship = new Friendship
                {
                    UserId = currentUserId,
                    FriendId = targetUserId,
                    Status = "blocked",
                    CreatedAt = DateTime.UtcNow
                };
                await _friendshipRepository.AddAsync(friendship);
            }
            return true;
        }

        public async Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId)
        {
            var friendship = await _friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUserId);
            
            // Chỉ người thực hiện chặn mới được quyền unblock
            if (friendship == null || friendship.Status != "blocked" || friendship.UserId != currentUserId) 
                return false;

            await _friendshipRepository.DeleteAsync(friendship);
            return true;
        }

        public async Task<IEnumerable<UserProfileDto>> GetFriendsListAsync(Guid currentUserId)
        {
            var friendships = await _friendshipRepository.GetFriendsAsync(currentUserId);
            
            // Map danh sách bạn bè
            return friendships.Select(f => 
            {
                var friendInfo = f.UserId == currentUserId ? f.Friend : f.User;
                return new UserProfileDto
                {
                    Id = friendInfo!.Id,
                    UserName = friendInfo.UserName,
                    Email = friendInfo.Email
                };
            });
        }

        public async Task<IEnumerable<FriendshipRequestDto>> GetPendingRequestsAsync(Guid currentUserId)
        {
            var requests = await _friendshipRepository.GetPendingRequestsAsync(currentUserId);
            
            return requests.Select(f => new FriendshipRequestDto
            {
                FriendshipId = f.Id,
                SenderId = f.User!.Id,
                SenderName = f.User.UserName,
                SentAt = f.CreatedAt
            });
        }
    }
}
```

---

## BƯỚC 5: WebApi Layer - APIs (`src/SavageExpenseTracker.WebApi`)

### 5.1 `Controllers/FriendshipsController.cs`
Lưu ý: Bạn sẽ cần logic lấy `Guid currentUserId` từ JWT Token. Đoạn code dưới đây giả lập việc lấy UserId từ User claims.

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/friendships")]
    // [Authorize] // Bỏ comment sau khi cài JWT
    public class FriendshipsController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipsController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        // Fake Helper: Lấy userId từ JWT (Bạn tự thay bằng logic thực tế của dự án)
        private Guid GetCurrentUserId() 
        {
            // return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // Hiện tại fix cứng để dev, đổi lại khi ráp JWT nhé!
            return Guid.Empty; 
        }

        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends()
        {
            var friends = await _friendshipService.GetFriendsListAsync(GetCurrentUserId());
            return Ok(friends);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _friendshipService.GetPendingRequestsAsync(GetCurrentUserId());
            return Ok(requests);
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendRequest(Guid targetUserId)
        {
            try
            {
                await _friendshipService.SendRequestAsync(GetCurrentUserId(), targetUserId);
                return Ok(new { Message = "Đã gửi lời mời kết bạn." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("accept/{id}")]
        public async Task<IActionResult> AcceptRequest(long id)
        {
            try
            {
                var success = await _friendshipService.AcceptRequestAsync(GetCurrentUserId(), id);
                if (!success) return NotFound();
                return Ok(new { Message = "Đã chấp nhận kết bạn." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> RejectRequest(long id)
        {
            try
            {
                var success = await _friendshipService.RejectRequestAsync(GetCurrentUserId(), id);
                if (!success) return NotFound();
                return Ok(new { Message = "Đã từ chối lời mời." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> Unfriend(Guid friendId)
        {
            var success = await _friendshipService.UnfriendAsync(GetCurrentUserId(), friendId);
            if (!success) return NotFound();
            return Ok(new { Message = "Đã hủy kết bạn." });
        }

        [HttpPost("block/{targetUserId}")]
        public async Task<IActionResult> BlockUser(Guid targetUserId)
        {
            try
            {
                await _friendshipService.BlockUserAsync(GetCurrentUserId(), targetUserId);
                return Ok(new { Message = "Đã chặn người dùng." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpDelete("unblock/{targetUserId}")]
        public async Task<IActionResult> UnblockUser(Guid targetUserId)
        {
            var success = await _friendshipService.UnblockUserAsync(GetCurrentUserId(), targetUserId);
            if (!success) return NotFound();
            return Ok(new { Message = "Đã bỏ chặn người dùng." });
        }
    }
}
```

### 5.2 Đăng ký Services trong `Program.cs`
Nhớ thêm 2 dòng này trước `var app = builder.Build();`:
```csharp
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
```
