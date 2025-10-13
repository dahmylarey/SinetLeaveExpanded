using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Models.ViewModels;
using SinetLeaveManagement.Services;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SinetLeaveManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        // ==========================
        // REGISTER
        // ==========================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");

                    // Generate email confirmation link
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account",
                        new { userId = user.Id, code }, protocol: Request.Scheme);

                    // 1️⃣ Send confirmation email
                    await _emailService.SendEmailAsync(model.Email, "Confirm your email",
                        $"<p>Hello {model.FirstName},</p>" +
                        $"<p>Thank you for registering on <strong>Sinet Leave Management</strong>.</p>" +
                        $"<p>Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</p>" +
                        $"<br/><p>-- Sinet Leave Management Team</p>");

                    // 2️⃣ Send welcome email (after registration)
                    await _emailService.SendEmailAsync(model.Email, "Welcome to Sinet Leave Application.",
                        $"<p>Dear {model.FirstName},</p>" +
                        $"<p>Welcome to <strong>Sinet Leave Management System</strong>!</p>" +
                        $"<p>You can now log in and start managing your leave requests efficiently.</p>" +
                        $"<br/><p>We're glad to have you on board!</p>" +
                        $"<p>-- The Sinet HR Team</p>");

                    TempData["Info"] = "Registration successful! Please check your email to confirm your account.";
                    return RedirectToAction("Login");
                }

                // If creation failed, show errors
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ==========================
        // CONFIRM EMAIL
        // ==========================
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
                return BadRequest("Invalid confirmation link.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Unable to load user with ID '{userId}'.");

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        // ==========================
        // LOGIN
        // ==========================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");

                ModelState.AddModelError("", "Invalid login attempt.");
            }
            return View(model);
        }

        // ==========================
        // LOGOUT
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> LogoutConfirm()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ==========================
        // FORGOT PASSWORD
        // ==========================
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Action(nameof(ResetPassword), "Account",
                    new { code, email = model.Email }, protocol: Request.Scheme);

                await _emailService.SendEmailAsync(model.Email, "Reset Password",
                    $"<p>Hello,</p>" +
                    $"<p>You requested to reset your password. You can reset it by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</p>" +
                    $"<p>If you didn’t request this, you can safely ignore this email.</p>" +
                    $"<br/><p>-- Sinet Leave Management System</p>");

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        // ==========================
        // RESET PASSWORD
        // ==========================
        [HttpGet]
        public IActionResult ResetPassword(string code = null, string email = null)
        {
            if (code == null || email == null)
                return BadRequest("A code and email must be supplied.");

            return View(new ResetPasswordViewModel { Code = code, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);

            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        // ==========================
        // DELETE ACCOUNT
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _signInManager.SignOutAsync();
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Account deleted successfully.";
                    return RedirectToAction("Register");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
