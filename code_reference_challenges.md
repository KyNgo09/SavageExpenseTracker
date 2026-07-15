# Code Reference: Tính năng Thi Đấu (Challenges)

Dưới đây là toàn bộ code tham khảo cho tính năng Thi đấu. Bạn có thể tạo file mới và gõ/copy theo để đảm bảo chuẩn kiến trúc Clean Architecture.

---

## 1. Domain Layer (`src/SavageExpenseTracker.Domain/Entities`)

### `Challenge.cs`
```csharp
using System;
using System.Collections.Generic;

namespace SavageExpenseTracker.Domain.Entities
{
    public class Challenge
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public Guid? WinnerId { get; set; }
        public Guid? LoserId { get; set; }
        public DateTime CreatedAt { get; set; }

        //Navigation properties
        public User? Winner { get; set; }
        public User? Loser { get; set; }
        public ICollection<ChallengeMember> ChallengeMembers { get; set; } = new List<ChallengeMember>();

        public string GetStatus()
        {
            var now = DateTime.UtcNow;
            if (now < DateStart) return "Upcoming";
            if (now > DateEnd) return "Finished";
            return "Active";
        }
    }
}
```

### `ChallengeMember.cs`
```csharp
using System;

namespace SavageExpenseTracker.Domain.Entities
{
    public class ChallengeMember
    {
        public long ChallengeId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public Challenge? Challenge { get; set; }
        public User? User { get; set; }
    }
}
```

---

## 2. Infrastructure Layer (`src/SavageExpenseTracker.Infrastructure`)

### Cập nhật `Data/SavageExpenseTrackerDbContext.cs`
Thêm 2 dòng `DbSet` vào class `SavageExpenseTrackerDbContext`:
```csharp
public DbSet<Challenge> Challenges => Set<Challenge>();
public DbSet<ChallengeMember> ChallengeMembers => Set<ChallengeMember>();
```
Thêm vào trong hàm `OnModelCreating(ModelBuilder modelBuilder)`:
```csharp
modelBuilder.Entity<Challenge>(entity =>
{
    entity.ToTable("challenges");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
    entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
    entity.Property(e => e.DateStart).HasColumnName("date_start").IsRequired();
    entity.Property(e => e.DateEnd).HasColumnName("date_end").IsRequired();
    entity.Property(e => e.WinnerId).HasColumnName("winner_id");
    entity.Property(e => e.LoserId).HasColumnName("loser_id");
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

    // ON DELETE SET NULL
    entity.HasOne(e => e.Winner)
        .WithMany()
        .HasForeignKey(e => e.WinnerId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.Loser)
        .WithMany()
        .HasForeignKey(e => e.LoserId)
        .OnDelete(DeleteBehavior.SetNull);
});

modelBuilder.Entity<ChallengeMember>(entity =>
{
    entity.ToTable("challenge_members");
    entity.HasKey(e => new { e.ChallengeId, e.UserId }); // Composite Key
    
    entity.Property(e => e.ChallengeId).HasColumnName("challenge_id");
    entity.Property(e => e.UserId).HasColumnName("user_id");
    entity.Property(e => e.JoinedAt).HasColumnName("joined_at").HasDefaultValueSql("NOW()");

    // ON DELETE CASCADE
    entity.HasOne(e => e.Challenge)
        .WithMany(c => c.ChallengeMembers)
        .HasForeignKey(e => e.ChallengeId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(e => e.User)
        .WithMany() // User ko cần collection ChallengeMembers trừ khi bạn muốn query ngược
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### `Repositories/ChallengeRepository.cs`
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
    public class ChallengeRepository : IChallengeRepository
    {
        private readonly SavageExpenseTrackerDbContext _context;

        public ChallengeRepository(SavageExpenseTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<Challenge?> GetByIdAsync(long id)
        {
            return await _context.Challenges
                .Include(c => c.ChallengeMembers)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Challenge challenge)
        {
            await _context.Challenges.AddAsync(challenge);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Challenge challenge)
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddMemberAsync(ChallengeMember member)
        {
            await _context.ChallengeMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(ChallengeMember member)
        {
            _context.ChallengeMembers.Remove(member);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Challenge>> GetUnprocessedFinishedChallengesAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Challenges
                .Include(c => c.ChallengeMembers)
                .Where(c => c.DateEnd < now && c.WinnerId == null)
                .ToListAsync();
        }
    }
}
```

---

## 3. Application Layer (`src/SavageExpenseTracker.Application`)

### `Interfaces/IChallengeRepository.cs`
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Domain.Entities;
using System;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IChallengeRepository
    {
        Task<Challenge?> GetByIdAsync(long id);
        Task AddAsync(Challenge challenge);
        Task UpdateAsync(Challenge challenge);
        Task AddMemberAsync(ChallengeMember member);
        Task RemoveMemberAsync(ChallengeMember member);
        Task<IEnumerable<Challenge>> GetUnprocessedFinishedChallengesAsync(); // Dành cho Cron Job
    }
}
```

### DTOs (`Dtos/Challenge/`)
Tạo các class Dto:
```csharp
// ChallengeDto.cs
public class ChallengeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
    public Guid? WinnerId { get; set; }
    public Guid? LoserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status => GetStatus();

    private string GetStatus()
    {
        var now = DateTime.UtcNow;
        if (now < DateStart) return "Upcoming";
        if (now > DateEnd) return "Finished";
        return "Active";
    }
}

// CreateChallengeDto.cs
public class CreateChallengeDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
}

// LeaderboardItemDto.cs
public class LeaderboardItemDto
{
    public Guid UserId { get; set; }
    public decimal TotalTimeWork { get; set; }
    public decimal TotalAmount { get; set; }
    public string Title { get; set; } = string.Empty; // "Thánh Sinh Tồn" hoặc "Báo Thủ" hoặc Rỗng
}
```

### `Interfaces/IChallengeService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Challenge;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IChallengeService
    {
        Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto dto);
        Task<bool> JoinChallengeAsync(long challengeId, Guid userId);
        Task<bool> LeaveChallengeAsync(long challengeId, Guid userId);
        Task<IEnumerable<LeaderboardItemDto>> GetLeaderboardAsync(long challengeId);
        Task ProcessFinishedChallengesAsync(); // Job sẽ gọi hàm này
    }
}
```

### `Interfaces/IChallengeNotificationService.cs`
```csharp
using System;
using System.Threading.Tasks;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IChallengeNotificationService
    {
        Task NotifyUserJoinedAsync(long challengeId, Guid userId);
        Task NotifyUserLeftAsync(long challengeId, Guid userId);
        Task NotifyChallengeEndedAsync(long challengeId, Guid winnerId, Guid loserId);
    }
}
```

---

## 4. SignalR & WebApi Layer (`src/SavageExpenseTracker.WebApi`)

### 4.1. Thêm Thư viện SignalR
Chạy lệnh này ở thư mục `.WebApi`:
`dotnet add package Microsoft.AspNetCore.SignalR`

### 4.2. Khởi tạo Hub (`Hubs/ChallengeHub.cs`)
```csharp
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SavageExpenseTracker.WebApi.Hubs
{
    public class ChallengeHub : Hub
    {
        // Khi client tham gia vào phòng xem Leaderboard
        public async Task JoinRoom(string challengeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Challenge_{challengeId}");
        }

        // Khi client thoát phòng
        public async Task LeaveRoom(string challengeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Challenge_{challengeId}");
        }
    }
}
```

### 4.3. Đăng ký Services & SignalR trong `Program.cs`
```csharp
// Thêm namespace Hubs và HostedServices
// using SavageExpenseTracker.WebApi.Hubs;
// using SavageExpenseTracker.WebApi.HostedServices;

// Đăng ký SignalR ở đoạn khai báo Services
builder.Services.AddSignalR();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IChallengeNotificationService, ChallengeNotificationService>();
builder.Services.AddHostedService<ChallengeClosingJob>(); // Job chạy ngầm

// Đăng ký Endpoint SignalR (sau app.MapControllers();)
app.MapHub<ChallengeHub>("/challengeHub");
```

### 4.4. Cấu hình Job chạy ngầm (`HostedServices/ChallengeClosingJob.cs`)
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.HostedServices
{
    public class ChallengeClosingJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ChallengeClosingJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var challengeService = scope.ServiceProvider.GetRequiredService<IChallengeService>();
                    await challengeService.ProcessFinishedChallengesAsync();
                }
                
                // Job quét 1 giờ 1 lần
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
```

### 4.5. `Controllers/ChallengesController.cs`
```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Dtos.Challenge;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/challenges")]
    public class ChallengesController : ControllerBase
    {
        private readonly IChallengeService _challengeService;

        public ChallengesController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        [HttpPost]
        public async Task<ActionResult<ChallengeDto>> Create(CreateChallengeDto dto)
        {
            var result = await _challengeService.CreateChallengeAsync(dto);
            return Ok(result);
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(long id, [FromBody] Guid userId)
        {
            try
            {
                var result = await _challengeService.JoinChallengeAsync(id, userId);
                if (!result) return NotFound(new { Message = "Phòng không tồn tại." });
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> Leave(long id, [FromBody] Guid userId)
        {
            try
            {
                var result = await _challengeService.LeaveChallengeAsync(id, userId);
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

*(Lưu ý: Logic Gửi Notification SignalR đã được tách ra dùng Dependency Inversion qua `IChallengeNotificationService` nhằm bảo đảm Clean Architecture).*

### 4.6. Cài đặt ChallengeNotificationService (`Services/ChallengeNotificationService.cs`)
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Hubs;

namespace SavageExpenseTracker.WebApi.Services
{
    public class ChallengeNotificationService : IChallengeNotificationService
    {
        private readonly IHubContext<ChallengeHub> _hubContext;

        public ChallengeNotificationService(IHubContext<ChallengeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyUserJoinedAsync(long challengeId, Guid userId)
        {
            await _hubContext.Clients.Group($"Challenge_{challengeId}").SendAsync("UserJoined", userId);
        }

        public async Task NotifyUserLeftAsync(long challengeId, Guid userId)
        {
            await _hubContext.Clients.Group($"Challenge_{challengeId}").SendAsync("UserLeft", userId);
        }

        public async Task NotifyChallengeEndedAsync(long challengeId, Guid winnerId, Guid loserId)
        {
            await _hubContext.Clients.Group($"Challenge_{challengeId}").SendAsync("ChallengeEnded", new { Winner = winnerId, Loser = loserId });
        }
    }
}
```

---

## 5. Cài đặt ChallengeService (`src/SavageExpenseTracker.Application/Services`)

Dưới đây là phần code bổ sung cho `ChallengeService.cs` nhằm hoàn thiện toàn bộ tính năng.

### `ChallengeService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IExpenseRepository _expenseRepository;
        private readonly IChallengeNotificationService _notificationService;

        public ChallengeService(
            IChallengeRepository challengeRepository,
            IExpenseRepository expenseRepository,
            IChallengeNotificationService notificationService
        )
        {
            _challengeRepository = challengeRepository;
            _expenseRepository = expenseRepository;
            _notificationService = notificationService;
        }

        public async Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto dto)
        {
            var challenge = new Challenge
            {
                Name = dto.Name,
                DateStart = dto.DateStart.ToUniversalTime(),
                DateEnd = dto.DateEnd.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow
            };

            await _challengeRepository.AddAsync(challenge);

            return MapToDto(challenge);
        }

        public async Task<bool> JoinChallengeAsync(long challengeId, Guid userId)
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge == null) return false;
            
            if (DateTime.UtcNow > challenge.DateEnd) 
                throw new InvalidOperationException("Thử thách đã kết thúc.");
            
            if (challenge.ChallengeMembers.Any(m => m.UserId == userId))
                throw new InvalidOperationException("User đã ở trong thử thách này.");

            var member = new ChallengeMember
            {
                ChallengeId = challengeId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            await _challengeRepository.AddMemberAsync(member);
            
            // Gửi Notification
            await _notificationService.NotifyUserJoinedAsync(challengeId, userId);

            return true;
        }

        public async Task<bool> LeaveChallengeAsync(long challengeId, Guid userId)
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge == null) return false;

            var member = challenge.ChallengeMembers.FirstOrDefault(m => m.UserId == userId);
            if (member == null) return false;

            await _challengeRepository.RemoveMemberAsync(member);
            
            // Gửi Notification
            await _notificationService.NotifyUserLeftAsync(challengeId, userId);

            return true;
        }

        public async Task<IEnumerable<LeaderboardItemDto>> GetLeaderboardAsync(long challengeId)
        {
            var challenge = await _challengeRepository.GetByIdAsync(challengeId);
            if (challenge == null) return new List<LeaderboardItemDto>();

            var leaderboard = new List<LeaderboardItemDto>();

            foreach (var member in challenge.ChallengeMembers)
            {
                var expenses = await _expenseRepository.GetByUserIdAsync(member.UserId);
                
                // Lọc Expense nằm trong khoảng thời gian của thử thách
                var validExpenses = expenses.Where(e => e.CreatedAt >= challenge.DateStart && e.CreatedAt <= challenge.DateEnd).ToList();
                
                var totalAmount = validExpenses.Sum(e => e.Amount);
                var totalTimeWork = validExpenses.Sum(e => e.TimeWork);

                leaderboard.Add(new LeaderboardItemDto
                {
                    UserId = member.UserId,
                    TotalAmount = totalAmount,
                    TotalTimeWork = totalTimeWork,
                    Title = totalAmount > 5000000 ? "Báo Thủ" : (totalAmount < 1000000 ? "Thánh Sinh Tồn" : "")
                });
            }

            return leaderboard.OrderBy(x => x.TotalAmount); // Sắp xếp theo số tiền (ai tiêu ít thì top 1)
        }

        public async Task ProcessFinishedChallengesAsync()
        {
            var finishedChallenges = await _challengeRepository.GetUnprocessedFinishedChallengesAsync();

            foreach (var challenge in finishedChallenges)
            {
                if (challenge.ChallengeMembers.Count > 0)
                {
                    var leaderboard = await GetLeaderboardAsync(challenge.Id);
                    
                    var winner = leaderboard.OrderBy(l => l.TotalAmount).First();
                    var loser = leaderboard.OrderByDescending(l => l.TotalAmount).First();

                    challenge.WinnerId = winner.UserId;
                    challenge.LoserId = loser.UserId;

                    await _challengeRepository.UpdateAsync(challenge);
                    
                    // Gửi thông báo kết thúc
                    await _notificationService.NotifyChallengeEndedAsync(challenge.Id, winner.UserId, loser.UserId);
                }
            }
        }

        private ChallengeDto MapToDto(Challenge challenge)
        {
            return new ChallengeDto
            {
                Id = challenge.Id,
                Name = challenge.Name,
                DateStart = challenge.DateStart,
                DateEnd = challenge.DateEnd,
                WinnerId = challenge.WinnerId,
                LoserId = challenge.LoserId,
                // Status property is calculated in Dto
            };
        }
    }
}
```
