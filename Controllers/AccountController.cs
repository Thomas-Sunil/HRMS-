using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims; // <-- Add this
using Microsoft.AspNetCore.Authentication; // <-- Add this
using Microsoft.AspNetCore.Authentication.Cookies; // <-- Add this
using System.Threading.Tasks; // <-- Add this

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

        // This method is now asynchronous
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == model.Username.ToLower());

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                // --- START: User Sign In Logic ---
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));
                // --- END: User Sign In Logic ---

                string redirectUrl = user.Role switch
                {
                    "hr" => Url.Action("Index", "Hr"),
                    "manager" => Url.Action("Index", "Manager"),
                    "employee" => Url.Action("Index", "Employee"),
                    _ => Url.Action("Index", "Home")
                };

                return Json(new { success = true, redirectUrl });
            }
            else
            {
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }

        // --- ADD THIS NEW LOGOUT ACTION ---
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}