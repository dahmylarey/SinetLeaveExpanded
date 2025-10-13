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
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // ✅ Apply pending migrations
            await context.Database.MigrateAsync();

            // ✅ Seed roles
            string[] roles = { "Admin", "Manager", "HR", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ✅ Seed default admin user
            var adminEmail = "admin@sinetservices.com";
            var adminPassword = "Admin@123";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    FullName = "Admin User",
                    Department = "HR",
                    JobTitle = "System Administrator",
                    EmployeeCode = "EMP-0001",
                    HireDate = DateTime.UtcNow,
                    ManagerName = "N/A"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"❌ Failed to create admin user: {errorMessage}");
                }

                await userManager.AddToRoleAsync(adminUser, "Admin");

                // ✅ Optionally seed related EmployeeProfile
                var profile = new EmployeeProfile
                {
                    UserId = adminUser.Id,
                    Address = "Head Office",
                    City = "Lagos",
                    Country = "Nigeria",
                    EmploymentStatus = "Active",
                    ContractStartDate = DateTime.UtcNow,
                    ContractEndDate = null
                };

                context.EmployeeProfiles.Add(profile);
                await context.SaveChangesAsync();
            }

            // ✅ Seed default leave types
            var leaveTypes = new[]
            {
                new LeaveType { Name = "Annual Leave" },
                new LeaveType { Name = "Sick Leave" },
                new LeaveType { Name = "Maternity Leave" },
                new LeaveType { Name = "Paternity Leave" }
            };

            foreach (var type in leaveTypes)
            {
                if (!await context.LeaveTypes.AnyAsync(t => t.Name == type.Name))
                {
                    await context.LeaveTypes.AddAsync(type);
                }
            }

            await context.SaveChangesAsync();

            Console.WriteLine("✅ Seed data completed successfully.");
        }
    }
}



//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using SinetLeaveManagement.Models;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace SinetLeaveManagement.Data
//{
//    public static class SeedData
//    {
//        public static async Task InitializeAsync(IServiceProvider serviceProvider)
//        {
//            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

//            // Apply pending migrations
//            await context.Database.MigrateAsync();

//            // ✅ Idempotent Role Seeding
//            string[] roles = { "Admin", "Manager", "HR", "User" };
//            foreach (var role in roles)
//            {
//                if (!await roleManager.RoleExistsAsync(role))
//                {
//                    await roleManager.CreateAsync(new IdentityRole(role));
//                }
//            }

//            // Admin credentials
//            var adminEmail = "admin@example.com";
//            var adminPassword = "Admin@123";

//            // ✅ Only create admin if not existing
//            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
//            if (existingAdmin == null)
//            {
//                var adminUser = new ApplicationUser
//                {
//                    UserName = adminEmail,
//                    Email = adminEmail,
//                    EmailConfirmed = true,
//                    FirstName = "Admin",
//                    LastName = "User",
//                    Department = "HR",
//                };

//                var result = await userManager.CreateAsync(adminUser, adminPassword);
//                if (result.Succeeded)
//                {
//                    await userManager.AddToRoleAsync(adminUser, "Admin");
//                }
//                else
//                {
//                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
//                }
//            }

//            // ✅ Idempotent Leave Types Seeding
//            var leaveTypesToSeed = new[]
//            {
//                new LeaveType { Name = "Annual Leave" },
//                new LeaveType { Name = "Sick Leave" },
//                new LeaveType { Name = "Maternity Leave" },
//                new LeaveType { Name = "Paternity Leave" }
//            };

//            foreach (var leaveType in leaveTypesToSeed)
//            {
//                if (!context.LeaveTypes.Any(lt => lt.Name == leaveType.Name))
//                {
//                    context.LeaveTypes.Add(leaveType);
//                }
//            }

//            await context.SaveChangesAsync();
//        }
//    }
//}
