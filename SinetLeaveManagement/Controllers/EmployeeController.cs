using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,HR")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // LIST ALL EMPLOYEE PROFILES
        // =========================
        public async Task<IActionResult> Index()
        {
            var profiles = await _context.EmployeeProfiles
                .Include(e => e.User)
                .ToListAsync();

            return View(profiles);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var profile = await _context.EmployeeProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }



        // =========================
        // CREATE EMPLOYEE PROFILE (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Show list of users who don’t have a profile yet
            var usersWithoutProfile = await _userManager.Users
                .Where(u => !_context.EmployeeProfiles.Any(p => p.UserId == u.Id))
                .ToListAsync();

            ViewBag.Users = usersWithoutProfile;
            return View();
        }

        // =========================
        // CREATE EMPLOYEE PROFILE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeProfile profile)
        {
            // Remove navigation property validation error
            ModelState.Remove("User");  // 👈 This line is crucial

            if (ModelState.IsValid)
            {
                _context.EmployeeProfiles.Add(profile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Employee profile created successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdown list if validation fails
            ViewBag.Users = await _userManager.Users
                .Where(u => !_context.EmployeeProfiles.Any(p => p.UserId == u.Id))
                .ToListAsync();

            return View(profile);
        }

        // =========================
        // EDIT EMPLOYEE PROFILE (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await _context.EmployeeProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }

        // =========================
        // EDIT EMPLOYEE PROFILE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeProfile profile)
        {
            if (!ModelState.IsValid)
                return View(profile);

            _context.Update(profile);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE PROFILE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var profile = await _context.EmployeeProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.EmployeeProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.EmployeeProfiles.Remove(profile);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee profile deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}




//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using SinetLeaveManagement.Data;
//using SinetLeaveManagement.Models;
//using System.Linq;
//using System.Threading.Tasks;

//namespace SinetLeaveManagement.Controllers
//{
//    [Authorize(Roles = "Admin,HR,Manager")]
//    public class EmployeeController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // GET: Employee
//        public async Task<IActionResult> Index()
//        {
//            var employees = await _context.Users
//                .Include(u => u.EmployeeProfile)
//                .OrderBy(u => u.FullName)
//                .ToListAsync();
//            return View(employees);
//        }

//        // GET: Employee/Details/5
//        public async Task<IActionResult> Details(string id)
//        {
//            if (id == null) return NotFound();

//            var employee = await _context.Users
//                .Include(u => u.EmployeeProfile)
//                .FirstOrDefaultAsync(u => u.Id == id);

//            if (employee == null) return NotFound();

//            return View(employee);
//        }

//        // GET: Employee/Edit/5
//        public async Task<IActionResult> Edit(string id)
//        {
//            if (id == null) return NotFound();

//            var employee = await _context.Users
//                .Include(u => u.EmployeeProfile)
//                .FirstOrDefaultAsync(u => u.Id == id);

//            if (employee == null) return NotFound();

//            return View(employee);
//        }

//        // POST: Employee/Edit/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(string id, ApplicationUser model)
//        {
//            if (id != model.Id) return NotFound();

//            var user = await _context.Users
//                .Include(u => u.EmployeeProfile)
//                .FirstOrDefaultAsync(u => u.Id == id);

//            if (user == null) return NotFound();

//            if (ModelState.IsValid)
//            {
//                user.FullName = model.FullName;
//                user.Department = model.Department;
//                user.JobTitle = model.JobTitle;
//                user.HireDate = model.HireDate;
//                user.DateOfBirth = model.DateOfBirth;
//                user.ManagerName = model.ManagerName;
//                user.EmployeeCode = model.EmployeeCode;

//                if (user.EmployeeProfile == null)
//                {
//                    user.EmployeeProfile = new EmployeeProfile();
//                }

//                user.EmployeeProfile.Address = model.EmployeeProfile?.Address;
//                user.EmployeeProfile.City = model.EmployeeProfile?.City;
//                user.EmployeeProfile.Country = model.EmployeeProfile?.Country;
//                user.EmployeeProfile.EmploymentStatus = model.EmployeeProfile?.EmploymentStatus;
//                user.EmployeeProfile.ContractStartDate = model.EmployeeProfile?.ContractStartDate;
//                user.EmployeeProfile.ContractEndDate = model.EmployeeProfile?.ContractEndDate;

//                _context.Update(user);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }

//            return View(model);
//        }

//        // GET: Employee/Delete/5
//        public async Task<IActionResult> Delete(string id)
//        {
//            if (id == null) return NotFound();

//            var employee = await _context.Users
//                .Include(u => u.EmployeeProfile)
//                .FirstOrDefaultAsync(u => u.Id == id);

//            if (employee == null) return NotFound();

//            return View(employee);
//        }

//        // POST: Employee/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(string id)
//        {
//            var employee = await _context.Users.FindAsync(id);
//            if (employee != null)
//            {
//                _context.Users.Remove(employee);
//                await _context.SaveChangesAsync();
//            }
//            return RedirectToAction(nameof(Index));
//        }
//    }
//}
