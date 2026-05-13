using Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<UserEntity>(options)
{
    public virtual DbSet<EventEntity> Events { get; set; } 
    public virtual DbSet<EventItemEntity> Items { get; set; } 
    public virtual DbSet<EventTaskEntity> Tasks { get; set; } 
    public virtual DbSet<EventItemAssignmentEntity> ItemAssignments { get; set; } 
    public virtual DbSet<EventTaskAssignmentEntity> TaskAssignments { get; set; } 
    public virtual DbSet<EventRoleEntity> EventRoles { get; set; } 
    public virtual DbSet<EventAttendanceEntity> Attendances { get; set; } 
    public virtual DbSet<EventChatEntity> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Soft Delete protection for Attendance.
        builder.Entity<EventAttendanceEntity>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        // Soft Delete protection for Roles
        builder.Entity<EventRoleEntity>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
