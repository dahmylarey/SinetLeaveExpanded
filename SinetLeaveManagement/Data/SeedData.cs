using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SinetLeaveManagement.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Apply pending migrations
            await context.Database.MigrateAsync();

            // ✅ Idempotent Role Seeding
            string[] roles = { "Admin", "Manager", "HR", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Admin credentials
            var adminEmail = "admin@example.com";
            var adminPassword = "Admin@123";

            // ✅ Only create admin if not existing
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // ✅ Idempotent Leave Types Seeding
            var leaveTypesToSeed = new[]
            {
                new LeaveType { Name = "Annual Leave" },
                new LeaveType { Name = "Sick Leave" },
                new LeaveType { Name = "Maternity Leave" },
                new LeaveType { Name = "Paternity Leave" }
            };

            foreach (var leaveType in leaveTypesToSeed)
            {
                if (!context.LeaveTypes.Any(lt => lt.Name == leaveType.Name))
                {
                    context.LeaveTypes.Add(leaveType);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
