using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // This was a missing directive
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;      // This was a missing directive
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Note: Use FirstOrDefaultAsync here
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                string redirectUrl = user.Role switch
                {
                    "hr" => Url.Action("Index", "Hr"),
                    "manager" => Url.Action("Index", "Manager"),
                    _ => Url.Action("Index", "Employee")
                };

                return Json(new { success = true, redirectUrl });
            }
            else
            {
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // --- CHANGE PASSWORD ACTIONS ---

        [HttpGet] // Added [HttpGet] for clarity
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = HttpContext.User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("OldPassword", "Incorrect current password.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been changed successfully.";

            return user.Role switch
            {
                "hr" => RedirectToAction("Index", "Hr"),
                "manager" => RedirectToAction("Index", "Manager"),
                _ => RedirectToAction("Index", "Employee"),
            };
        }
    }
}