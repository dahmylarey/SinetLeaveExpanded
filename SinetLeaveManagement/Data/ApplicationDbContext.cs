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

        // DbSets for your entities
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);




            // Configure relationship (RequestingUser has many LeaveRequests)
            builder.Entity<LeaveRequest>()
                .HasOne(l => l.RequestingUser)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(l => l.RequestingUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
