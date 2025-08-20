using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authorization; // <-- Add this
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    [Authorize(Roles = "manager, hr")] // <-- Secure the whole controller
    public class ManagerController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // This method now finds the currently authenticated user
        private async Task<Employee> GetCurrentManagerAsync()
        {
            // HttpContext.User.Identity.Name gets the username from the secure cookie
            var username = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            return await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        public async Task<IActionResult> Index()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Content("Could not find an employee profile for the logged-in manager.");

            // Show a different message if manager isn't in a department yet.
            if (manager.DepartmentId == null)
            {
                // Create a view model with empty lists to avoid a crash on the view
                var emptyViewModel = new ManagerDashboardViewModel
                {
                    Manager = manager,
                    TeamMembers = new List<Employee>(), // Empty list
                    UnassignedEmployees = new List<Employee>() // Empty list
                };
                ViewBag.ErrorMessage = "You are not currently assigned to a department. Please contact HR.";
                return View(emptyViewModel);
            }

            // The rest of the logic remains the same
            var teamMembers = await _context.Employees
                .Where(e => e.DepartmentId == manager.DepartmentId)
                .ToListAsync();

            var unassignedEmployees = await _context.Employees
                .Where(e => e.DepartmentId == null)
                .ToListAsync();

            var viewModel = new ManagerDashboardViewModel
            {
                Manager = manager,
                TeamMembers = teamMembers,
                UnassignedEmployees = unassignedEmployees
            };

            return View(viewModel);
        }
        // GET: /Manager/Projects/5 (for a specific employee)
        public async Task<IActionResult> Projects(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var projects = await _context.Projects
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.Deadline)
                .ToListAsync();

            var viewModel = new EmployeeProjectsViewModel
            {
                Employee = employee,
                Projects = projects
            };

            return View(viewModel);
        }

        // POST: /Manager/Projects/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Projects(int id, EmployeeProjectsViewModel viewModel)
        {
            var employee = await _context.Employees.FindAsync(id);
            var manager = await GetCurrentManagerAsync();
            if (employee == null || manager == null) return NotFound();

            if (!string.IsNullOrEmpty(viewModel.NewProjectName))
            {
                var newProject = new Project
                {
                    Name = viewModel.NewProjectName,
                    Description = viewModel.NewProjectDescription,
                    Deadline = viewModel.NewProjectDeadline,
                    Status = "Assigned",
                    EmployeeId = employee.Id,
                    ManagerId = manager.Id
                };
                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();

                // Redirect back to the same page to see the new project in the list
                return RedirectToAction(nameof(Projects), new { id = employee.Id });
            }

            // If model state is invalid, we need to repopulate the view
            viewModel.Employee = employee;
            viewModel.Projects = await _context.Projects
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.Deadline)
                .ToListAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToTeam(int employeeId)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) return RedirectToAction(nameof(Index));

            var employeeToAssign = await _context.Employees.FindAsync(employeeId);

            if (employeeToAssign != null && employeeToAssign.DepartmentId == null)
            {
                employeeToAssign.DepartmentId = manager.DepartmentId;
                _context.Update(employeeToAssign);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}