using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using BCrypt.Net; // <-- 1. Add the BCrypt using statement

namespace hrms.Controllers
{
    public class AccountController : Controller
    {
        // 2. This will hold our database connection
        private readonly ApplicationDbContext _context;

        // 3. The database connection is "injected" into the controller when it's created
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // This method just displays the login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // This method handles the actual login logic
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            // 4. Find a user in the database with a matching username.
            //    Note: We must use ToLower() here because PostgreSQL is case-sensitive
            //    and we want to allow users to log in with 'HR_Admin', 'hr_admin', etc.
            var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == model.Username.ToLower());

            // 5. Check if the user exists AND if the password is correct
            //    BCrypt.Verify will compare the plain-text password from the form
            //    with the hash stored in the database.
            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                // --- Login is successful ---
                string redirectUrl;

                // 6. Redirect based on the role we got from the database
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
                // --- Login failed ---
                // 7. If user is null or password verification fails, return an error
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }
    }
}