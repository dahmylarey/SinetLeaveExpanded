using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Models.ViewModels;
using SinetLeaveManagement.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,HR")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager,
                               IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // ======================
        // DISPLAY ALL USERS
        // ======================
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // ======================
        // CREATE USER (GET)
        // ======================
        [HttpGet]
        public IActionResult Create()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            var model = new CreateUserViewModel
            {
                Roles = roles
            };
            return View(model);
        }

        // ======================
        // CREATE USER (POST)
        // ======================
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            // Check for duplicate email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "A user with this email already exists.");
                model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            // Ensure required fields are not null
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? "Unknown" : model.FirstName,
                LastName = string.IsNullOrWhiteSpace(model.LastName) ? "Unknown" : model.LastName,
                Department = model.Department, // optional
                JobTitle = model.JobTitle      // optional
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.SelectedRole))
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);

                // ==========================
                // EMAIL NOTIFICATION
                // ==========================
                try
                {
                    string subject = "Welcome to SINET Leave Management";
                    string body = $@"
                        <h3>Welcome, {user.FirstName}!</h3>
                        <p>Your account has been created successfully.</p>
                        <p><strong>Email:</strong> {user.Email}</p>
                        <p>You can now log in to the portal and manage your leave requests.</p>
                        <p>Thank you,<br/>SINET HR Team</p>";
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }
                catch
                {
                    // Email sending failed — log if needed
                }

                TempData["Success"] = "User created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        // ======================
        // EDIT USER (GET)
        // ======================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Department = user.Department,
                JobTitle = user.JobTitle
            };

            return View(model);
        }

        // ======================
        // EDIT USER (POST)
        // ======================
        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Department = model.Department;
            user.JobTitle = model.JobTitle;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "User details updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ======================
        // DELETE USER (GET CONFIRM)
        // ======================
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // ======================
        // DELETE USER (POST)
        // ======================
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var hasLeaveRequests = await _context.LeaveRequests
                .AnyAsync(lr => lr.RequestingUserId == id);

            if (hasLeaveRequests)
            {
                TempData["ErrorMessage"] = "Cannot delete this user because they have associated leave requests.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View("Delete", user);
        }

        // ======================
        // EDIT USER ROLES
        // ======================
        [HttpGet]
        public async Task<IActionResult> EditRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var roles = _roleManager.Roles.ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditRolesViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email,
                AssignedRoles = userRoles,
                AllRoles = roles.Select(r => r.Name).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRoles(EditRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            var userRoles = await _userManager.GetRolesAsync(user);

            var rolesToAdd = model.SelectedRoles.Except(userRoles);
            var rolesToRemove = userRoles.Except(model.SelectedRoles);

            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            TempData["Success"] = "User roles updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ======================
        // CHANGE PASSWORD
        // ======================
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (NewPassword != ConfirmPassword)
            {
                TempData["SuccessMessage"] = "New password and confirmation do not match.";
                return RedirectToAction("Profile");
            }

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password updated successfully.";
            }
            else
            {
                TempData["SuccessMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Profile");
        }
    }
}



//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using SinetLeaveManagement.Data;
//using SinetLeaveManagement.Models;
//using SinetLeaveManagement.Models.ViewModels;
//using SinetLeaveManagement.Services;
//using Microsoft.EntityFrameworkCore;

//using System.Linq;
//using System.Threading.Tasks;

//namespace SinetLeaveManagement.Controllers
//{
//    [Authorize(Roles = "Admin,Manager,HR")]
//    public class AdminController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly RoleManager<IdentityRole> _roleManager;
//        private readonly IEmailService _emailService;

//        public AdminController(ApplicationDbContext context,
//                               UserManager<ApplicationUser> userManager,
//                               RoleManager<IdentityRole> roleManager,
//                               IEmailService emailService)
//        {
//            _context = context;
//            _userManager = userManager;
//            _roleManager = roleManager;
//            _emailService = emailService;
//        }

//        // ======================
//        // READ: Display all users
//        // ======================
//        public IActionResult Index()
//        {
//            var users = _userManager.Users.ToList();
//            return View(users);
//        }

//        // ======================
//        // CREATE USER (GET)
//        // ======================
//        [HttpGet]
//        public async Task<IActionResult> Create()
//        {
//            var roles = await _roleManager.Roles
//                .Select(r => r.Name)
//                .ToListAsync();

//            var model = new CreateUserViewModel
//            {
//                Roles = roles
//            };

//            return View(model);
//        }



//        // ======================
//        // CREATE USER (POST)
//        // ======================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(CreateUserViewModel model)
//        {
//            if (!ModelState.IsValid)
//            {
//                // reload roles in case of error
//                model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
//                return View(model);
//            }

//            // ✅ Step 1: Check if email or username already exists
//            var existingUser = await _userManager.FindByEmailAsync(model.Email);
//            if (existingUser != null)
//            {
//                ModelState.AddModelError("Email", "A user with this email already exists.");
//                model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
//                return View(model);
//            }

//            // ✅ Step 2: Create new user
//            var user = new ApplicationUser
//            {
//                UserName = model.Email.ToUpper(),
//                Email = model.Email,
//                FullName = model.FullName,
//                EmailConfirmed = true
//            };

//            var result = await _userManager.CreateAsync(user, model.Password);

//            if (result.Succeeded)
//            {
//                // ✅ Step 3: Assign Role
//                if (!string.IsNullOrEmpty(model.SelectedRole))
//                {
//                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
//                }

//                // ✅ Optional: Create EmployeeProfile entry if needed
//                // var profile = new EmployeeProfile
//                // {
//                //     UserId = user.Id,
//                //     EmploymentStatus = "Active"
//                // };
//                // _context.EmployeeProfiles.Add(profile);
//                // await _context.SaveChangesAsync();

//                TempData["Success"] = "User created successfully!";
//                return RedirectToAction("Index");
//            }

//            // ✅ Handle creation errors
//            foreach (var error in result.Errors)
//            {
//                ModelState.AddModelError("", error.Description);
//            }

//            model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
//            return View(model);
//        }




//        // ======================
//        // EDIT USER DETAILS (GET)
//        // ======================
//        [HttpGet]
//        public async Task<IActionResult> Edit(string id)
//        {
//            var user = await _userManager.FindByIdAsync(id);
//            if (user == null)
//                return NotFound();

//            var model = new EditUserViewModel
//            {
//                Id = user.Id,
//                FirstName = user.FirstName,
//                LastName = user.LastName,
//                Email = user.Email
//            };

//            return View(model);
//        }

//        // ======================
//        // EDIT USER DETAILS (POST)
//        // ======================
//        [HttpPost]
//        public async Task<IActionResult> Edit(EditUserViewModel model)
//        {
//            if (!ModelState.IsValid) return View(model);

//            var user = await _userManager.FindByIdAsync(model.Id);
//            if (user == null)
//                return NotFound();

//            user.FirstName = model.FirstName;
//            user.LastName = model.LastName;
//            user.Email = model.Email;
//            user.UserName = model.Email;

//            var result = await _userManager.UpdateAsync(user);

//            if (result.Succeeded)
//            {
//                TempData["Success"] = "User details updated successfully.";
//                return RedirectToAction(nameof(Index));
//            }

//            foreach (var error in result.Errors)
//                ModelState.AddModelError("", error.Description);

//            return View(model);
//        }

//        // ======================
//        // DELETE USER (GET CONFIRM)
//        // ======================
//        [HttpGet]
//        public async Task<IActionResult> Delete(string id)
//        {
//            if (id == null)
//                return NotFound();

//            var user = await _userManager.FindByIdAsync(id);
//            if (user == null)
//                return NotFound();

//            return View(user); // Confirm deletion
//        }

//        // ======================
//        // DELETE USER (POST)
//        // ======================
//        [HttpPost, ActionName("DeleteConfirmed")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(string id)
//        {
//            if (id == null)
//                return NotFound();

//            var user = await _userManager.FindByIdAsync(id);
//            if (user == null)
//                return NotFound();

//            // Check for existing leave requests
//            var hasLeaveRequests = await _context.LeaveRequests
//                .AnyAsync(lr => lr.RequestingUserId == id);

//            if (hasLeaveRequests)
//            {
//                TempData["ErrorMessage"] = "Cannot delete this user because they have associated leave requests.";
//                return RedirectToAction(nameof(Index));
//            }

//            var result = await _userManager.DeleteAsync(user);
//            if (result.Succeeded)
//            {
//                TempData["SuccessMessage"] = "User deleted successfully.";
//                return RedirectToAction(nameof(Index));
//            }

//            foreach (var error in result.Errors)
//            {
//                ModelState.AddModelError("", error.Description);
//            }

//            return View("Delete", user);
//        }


//        // ======================
//        // EDIT USER ROLES (EXISTING)
//        // ======================
//        public async Task<IActionResult> EditRoles(string userId)
//        {
//            var user = await _userManager.FindByIdAsync(userId);
//            var roles = _roleManager.Roles.ToList();
//            var userRoles = await _userManager.GetRolesAsync(user);

//            var model = new EditRolesViewModel
//            {
//                UserId = user.Id,
//                UserEmail = user.Email,
//                AssignedRoles = userRoles,
//                AllRoles = roles.Select(r => r.Name).ToList()
//            };

//            return View(model);
//        }

//        [HttpPost]
//        public async Task<IActionResult> EditRoles(EditRolesViewModel model)
//        {
//            var user = await _userManager.FindByIdAsync(model.UserId);
//            var userRoles = await _userManager.GetRolesAsync(user);

//            var rolesToAdd = model.SelectedRoles.Except(userRoles);
//            var rolesToRemove = userRoles.Except(model.SelectedRoles);

//            await _userManager.AddToRolesAsync(user, rolesToAdd);
//            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

//            TempData["Success"] = "User roles updated successfully.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ======================
//        // PROFILE (OPTIONAL)
//        // ======================




//        [HttpPost]
//        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
//        {
//            var user = await _userManager.GetUserAsync(User);

//            if (NewPassword != ConfirmPassword)
//            {
//                TempData["SuccessMessage"] = "New password and confirmation do not match.";
//                return RedirectToAction("Profile");
//            }

//            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);

//            if (result.Succeeded)
//            {
//                TempData["SuccessMessage"] = "Password updated successfully.";
//            }
//            else
//            {
//                TempData["SuccessMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
//            }

//            return RedirectToAction("Profile");
//        }

//    }
//}
