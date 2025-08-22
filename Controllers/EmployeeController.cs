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
        public async Task<IActionResult> MyAttendance()
        {
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null) return Unauthorized();

            var attendanceRecords = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id)
                .OrderByDescending(a => a.Date)
                .Take(30) // Show the last 30 records for performance
                .ToListAsync();

            return View(attendanceRecords);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetMyAttendanceData()
        {
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null) return Unauthorized();

            var events = new List<object>();

            // 1. Fetch all actual attendance and leave records for the employee
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id)
                .ToDictionaryAsync(a => a.Date, a => a.Status); // Use a dictionary for fast lookups

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == employee.Id && lr.Status.Contains("Approved"))
                .ToListAsync();

            // 2. Loop through every day from joining date until today
            for (var day = employee.DateOfJoining.Date; day.Date <= DateTime.Today; day = day.AddDays(1))
            {
                // 3. Skip weekends (Saturday and Sunday)
                if (day.DayOfWeek == DayOfWeek.Sunday || day.DayOfWeek == DayOfWeek.Saturday)
                {
                    continue;
                }

                // 4. Check if the user had an attendance record for this day
                if (attendances.TryGetValue(day, out var status))
                {
                    // User was present, add a green event
                    events.Add(new
                    {
                        title = status,
                        start = day.ToString("yyyy-MM-dd"),
                        backgroundColor = "#198754",
                        borderColor = "#198754"
                    });
                }
                // 5. Check if the user was on approved leave for this day
                else if (leaveRequests.Any(lr => day >= lr.StartDate.Date && day <= lr.EndDate.Date))
                {
                    // User was on leave, add a yellow event
                    events.Add(new
                    {
                        title = "On Leave",
                        start = day.ToString("yyyy-MM-dd"),
                        backgroundColor = "#ffc107",
                        borderColor = "#ffc107"
                    });
                }
                else
                {
                    // 6. If no attendance and no leave, the employee was absent. Add a red event.
                    events.Add(new
                    {
                        title = "Absent",
                        start = day.ToString("yyyy-MM-dd"),
                        backgroundColor = "#dc3545",
                        borderColor = "#dc3545"
                    });
                }
            }
            return Json(events);
        }

    }
}