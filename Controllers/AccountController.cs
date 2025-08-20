using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
        public IActionResult Login(LoginViewModel model)
        {
            // This is the line that was missing. It finds the user in the database
            // using a case-insensitive comparison that Entity Framework can translate to SQL.
            var user = _context.Users.FirstOrDefault(u =>
                u.Username.ToLower() == model.Username.ToLower());

            // Check if the user exists and if the password is correct
            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                string redirectUrl;

                switch (user.Role)
                {
                    case "hr":
                        redirectUrl = Url.Action("Index", "Hr");
                        break;
                    case "manager":
                        redirectUrl = Url.Action("Index", "Manager");
                        break;
                    case "employee":
                        redirectUrl = Url.Action("Index", "Employee");
                        break;
                    default:
                        // Default to the home page if role is unknown
                        redirectUrl = Url.Action("Index", "Home");
                        break;
                }

                return Json(new { success = true, redirectUrl = redirectUrl });
            }
            else
            {
                // If user is null or password fails, return an error
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }
    }
}