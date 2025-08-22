using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace hrms.Controllers
{
    [Authorize(Roles = "manager,hr")]
    public class ManagerController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        private async Task<Employee> GetCurrentManagerAsync()
        {
            var username = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            return await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        // GET: /Manager/Index
        public async Task<IActionResult> Index()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null)
            {
                ViewBag.ErrorMessage = "Could not find a valid employee profile for the logged-in manager.";
                return View(new List<Employee>());
            }

            if (manager.DepartmentId == null)
            {
                ViewBag.ErrorMessage = "You are not assigned to a department. Please contact HR.";
                return View(new List<Employee>());
            }

            var teamMembers = await _context.Employees
                .Where(e => e.DepartmentId == manager.DepartmentId)
                .Include(e => e.Projects)
                .ToListAsync();

            return View(teamMembers ?? new List<Employee>());
        }

        // GET: /Manager/Assign
        public async Task<IActionResult> Assign()
        {
            var unassignedEmployees = await _context.Employees
                .Where(e => e.DepartmentId == null)
                .ToListAsync();

            return View(unassignedEmployees ?? new List<Employee>());
        }

        // POST: /Manager/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int employeeId)
        {
            var manager = await GetCurrentManagerAsync();
            var employee = await _context.Employees.FindAsync(employeeId);
            if (manager?.DepartmentId != null && employee?.DepartmentId == null)
            {
                employee.DepartmentId = manager.DepartmentId;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Assign));
        }

        // GET: /Manager/Projects
        public async Task<IActionResult> Projects()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return NotFound();

            var projects = await _context.Projects
                .Where(p => p.ManagerId == manager.Id)
                .Include(p => p.AssignedEmployees)
                .ToListAsync();

            return View(projects ?? new List<Project>());
        }

        // POST: /Manager/CreateProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(string projectName, string projectDescription, DateTime? projectDeadline)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return NotFound();

            var project = new Project
            {
                Name = projectName,
                Description = projectDescription,
                Deadline = projectDeadline,
                ManagerId = manager.Id,
                Status = "Not Started"
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Projects));
        }

        // GET: /Manager/ProjectDetails/{id}
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return NotFound();

            var project = await _context.Projects.Include(p => p.AssignedEmployees).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            var availableMembers = await _context.Employees
                .Where(e => e.DepartmentId == manager.DepartmentId && !e.Projects.Any())
                .ToListAsync();

            var viewModel = new ProjectDetailsViewModel
            {
                Project = project,
                AvailableTeamMembers = availableMembers ?? new List<Employee>()
            };
            return View(viewModel);
        }

        // POST: /Manager/AssignToProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToProject(int projectId, int employeeId)
        {
            var project = await _context.Projects.Include(p => p.AssignedEmployees).FirstOrDefaultAsync(p => p.Id == projectId);
            var employee = await _context.Employees.FindAsync(employeeId);
            if (project != null && employee != null)
            {
                project.AssignedEmployees.Add(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ProjectDetails), new { id = projectId });
        }

        // POST: /Manager/UnassignFromTeam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignFromTeam(int employeeId)
        {
            var manager = await GetCurrentManagerAsync();
            var employee = await _context.Employees.FindAsync(employeeId);
            if (manager?.DepartmentId != null && employee?.DepartmentId == manager.DepartmentId)
            {
                employee.DepartmentId = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Manager/DeleteProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            var manager = await GetCurrentManagerAsync();
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null && project.ManagerId == manager.Id)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Projects));
        }

        // POST: /Manager/UnassignFromProject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignFromProject(int projectId, int employeeId)
        {
            var project = await _context.Projects.Include(p => p.AssignedEmployees).FirstOrDefaultAsync(p => p.Id == projectId);
            var employee = await _context.Employees.FindAsync(employeeId);
            if (project != null && employee != null && project.AssignedEmployees.Any(e => e.Id == employeeId))
            {
                project.AssignedEmployees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ProjectDetails), new { id = projectId });
        }

        // GET: /Manager/TeamAttendance
        public async Task<IActionResult> TeamAttendance()
        {
            var manager = await GetCurrentManagerAsync();
            ViewBag.DepartmentName = manager?.Department?.Name ?? "Your Team";
            return View();
        }

        // GET: /Manager/GetTeamAttendanceData
        [HttpGet]
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetTeamAttendanceData(DateTime start, DateTime end, int? employeeId = null)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) return Unauthorized();

            // Determine which employees to fetch data for
            var teamMemberQuery = _context.Employees.Where(e => e.DepartmentId == manager.DepartmentId);
            if (employeeId.HasValue)
            {
                // If a specific employee is requested (for the modal), filter to just that one
                teamMemberQuery = teamMemberQuery.Where(e => e.Id == employeeId.Value);
            }

            var teamMembers = await teamMemberQuery.ToListAsync();
            var teamMemberIds = teamMembers.Select(tm => tm.Id).ToList();

            if (!teamMemberIds.Any())
            {
                return Json(new List<object>());
            }

            // Fetch actual attendance records
            var attendances = await _context.Attendances
                .Where(a => teamMemberIds.Contains(a.EmployeeId) && a.Date >= start && a.Date <= end)
                .ToDictionaryAsync(a => (a.EmployeeId, a.Date)); // Use a dictionary for fast lookup

            // ---- THIS IS THE COMPLETED LOGIC ----
            // Fetch approved leave requests for the same employees and date range
            var leaveRequests = await _context.LeaveRequests
                .Where(lr => teamMemberIds.Contains(lr.EmployeeId)
                             && lr.Status.Contains("Approved")
                             && lr.StartDate.Date <= end.Date
                             && lr.EndDate.Date >= start.Date)
                .ToListAsync();
            // ---- END COMPLETED LOGIC ----

            var events = new List<object>();

            // Loop through each relevant day and employee to generate absent/leave events
            foreach (var employee in teamMembers)
            {
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    if (day < employee.DateOfJoining.Date || day > DateTime.Today || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }

                    // Check if an event already exists for this day (from attendance)
                    if (attendances.ContainsKey((employee.Id, day)))
                    {
                        continue; // Skip, because a "Present" record already exists
                    }

                    // Check if the user was on approved leave
                    if (leaveRequests.Any(lr => lr.EmployeeId == employee.Id && day >= lr.StartDate.Date && day <= lr.EndDate.Date))
                    {
                        events.Add(new
                        {
                            title = employeeId.HasValue ? "On Leave" : $"{employee.FullName} - On Leave",
                            start = day.ToString("yyyy-MM-dd"),
                            backgroundColor = "#ffc107", // Yellow
                            borderColor = "#ffc107"
                        });
                    }
                    else
                    {
                        // If no attendance and no leave, they were absent
                        events.Add(new
                        {
                            title = employeeId.HasValue ? "Absent" : $"{employee.FullName} - Absent",
                            start = day.ToString("yyyy-MM-dd"),
                            backgroundColor = "#dc3545", // Red
                            borderColor = "#dc3545"
                        });
                    }
                }
            }

            // Now, add the "Present" events from the attendance records
            events.AddRange(attendances.Values.Select(a => new {
                title = employeeId.HasValue ? a.Status : $"{teamMembers.FirstOrDefault(e => e.Id == a.EmployeeId)?.FullName} - {a.Status}",
                start = a.Date.ToString("yyyy-MM-dd"),
                backgroundColor = a.Status == "Present" ? "#0d6efd" : "#6c757d", // Blue for present
                borderColor = a.Status == "Present" ? "#0d6efd" : "#6c757d"
            }));

            return Json(events);
        }
    }
}