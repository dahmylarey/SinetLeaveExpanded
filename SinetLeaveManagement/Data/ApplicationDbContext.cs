using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Models;

namespace SinetLeaveManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Notification: User ↔ Notifications
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest: RequestingUser ↔ LeaveRequests
            builder.Entity<LeaveRequest>()
                .HasOne(l => l.RequestingUser)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(l => l.RequestingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 Removed: builder.Entity<AuditLog>().HasOne...
            // Since PerformedBy is just a string, no relationship needed
        }
    }
}
