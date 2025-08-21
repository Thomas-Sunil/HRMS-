using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    [Authorize(Roles = "employee,manager,hr")] // Authorize all roles
    public class EmployeeController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // Helper to get the employee for the currently logged in user
        private async Task<Employee> GetCurrentUserEmployeeAsync()
        {
            var username = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        public async Task<IActionResult> Index()
        {
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null)
            {
                // This is a critical state - user is logged in but has no employee profile
                return Content("Error: Your user account is not linked to an employee profile.");
            }

            // Fetch projects related to this employee
            var projects = await _context.Projects
                .Where(p => p.AssignedEmployees.Any(e => e.Id == employee.Id))
                .Include(p => p.ProjectTasks)
                .ToListAsync();

            // Store the projects in the ViewBag, ensuring it's never null
            ViewBag.Projects = projects ?? new List<Project>();

            // Pass the employee object as the model to the view
            return View(employee);
        }
    }
}