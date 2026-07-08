using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Infrastructure.Data
{
    public class SavageExpenseTrackerDbContext : DbContext
    {
        public SavageExpenseTrackerDbContext(DbContextOptions<SavageExpenseTrackerDbContext> options) 
            : base(options)
        {
        }

        // Định nghĩa DbSet cho bảng Users
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Fluent API để ánh xạ với bảng PostgreSQL tên "users" (viết thường)
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // Tên bảng dưới Postgres
                
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
                entity.Property(e => e.HourlyRate).HasColumnName("hourly_rate").HasPrecision(18, 2).HasDefaultValue(20000);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

                // Ràng buộc Unique
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });
        }
    }
}
