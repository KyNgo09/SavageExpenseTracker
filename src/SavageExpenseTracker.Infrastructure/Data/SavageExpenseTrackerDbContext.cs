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
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Fluent API to map to a PostgreSQL table named "users" (lowercase).
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // Table name in Postgres
                
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
                entity.Property(e => e.UserName).HasColumnName("username").IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
                entity.Property(e => e.HourlyRate).HasColumnName("hourly_rate").HasPrecision(18, 2).HasDefaultValue(20000);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

                // Unique Constraints
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure table named "expenses".
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.ToTable("expenses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn(); // GENERATED ALWAYS AS IDENTITY
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(512);
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
                entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.TimeWork).HasColumnName("time_work").HasPrecision(5, 2).IsRequired();
                entity.Property(e => e.SavageComment).HasColumnName("savage_comment").HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
                entity.Property(e => e.AppliedHourlyRate).HasColumnName("applied_hourly_rate").HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.CategoryId).HasColumnName("category_id").IsRequired();
                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Expenses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Expenses)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(entity => 
            {
                entity.ToTable("categories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }
    }
}
