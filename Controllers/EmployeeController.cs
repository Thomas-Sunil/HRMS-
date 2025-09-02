using hrms.Data;
using hrms.Models;
using hrms.ViewModels;
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
            if (employee == null) return Content("Error: User not linked to an employee profile.");

            // ---- THIS IS THE FIX ----
            // We now filter for projects where the status is NOT 'Completed'.
            ViewBag.ActiveProjects = await _context.Projects
                .Where(p => p.AssignedEmployees.Any(e => e.Id == employee.Id) &&
                              p.Status != "Completed") // This is the new condition
                .ToListAsync();
            // ---- END FIX ----

            // --- This part remains the same ---
            ViewBag.CompletedProjects = await _context.PerformanceReviews
                .Include(r => r.Project)
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            ViewBag.MyLeaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == employee.Id)
                .OrderByDescending(lr => lr.RequestDate).Take(5).ToListAsync();

            ViewBag.PendingInvitations = await _context.MeetingInvitations
                .Include(i => i.Meeting)
                .Where(i => i.EmployeeId == employee.Id && i.Status == "Pending")
                .OrderBy(i => i.Meeting.StartTime)
                .ToListAsync();

            ViewBag.AcceptedMeetings = await _context.MeetingInvitations
                .Include(i => i.Meeting)
                .Where(i => i.Status == "Accepted" && i.Meeting.StartTime.Date >= DateTime.Today)
                .ToListAsync();

            return View(employee);
        }
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var employee = await GetCurrentUserEmployeeAsync();
            var project = await _context.Projects.FindAsync(id);
            // Security check: an employee can only view a project they are assigned to
            bool isAssigned = await _context.Entry(project)
                .Collection(p => p.AssignedEmployees).Query().AnyAsync(e => e.Id == employee.Id);

            if (!isAssigned) return Unauthorized();

            var myTasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == id && t.AssignedEmployeeId == employee.Id)
                .OrderBy(t => t.IsCompleted)
                .ToListAsync();

            var viewModel = new EmployeeProjectViewModel
            {
                Project = project,
                MyTasks = myTasks
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTaskStatus(int taskId)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            var employee = await GetCurrentUserEmployeeAsync();

            // Security check: Can only toggle tasks assigned to you
            if (task != null && task.AssignedEmployeeId == employee.Id)
            {
                task.IsCompleted = !task.IsCompleted; // Flip the status
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ProjectDetails), new { id = task.ProjectId });
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> RespondToInvitation(int invitationId, string status)
        {
            var employee = await GetCurrentUserEmployeeAsync();
            var invitation = await _context.MeetingInvitations.FindAsync(invitationId);

            // Security: ensure user is responding to their own invitation
            if (invitation != null && invitation.EmployeeId == employee.Id && (status == "Accepted" || status == "Declined"))
            {
                invitation.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MyAttendance()
        {
            // The view needs the Employee object to display the title.
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null)
            {
                // This handles cases where the user might not have an employee profile yet.
                return Content("Unable to find an employee profile for your user account.");
            }

            // --- THIS IS THE FIX ---
            // Pass the single 'employee' object as the model to the view.
            // DO NOT pass the list of attendance records.
            return View(employee);
            // --- END FIX ---
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetMyAttendanceData(DateTime start, DateTime end)
        {
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null) return Unauthorized();

            var events = new List<object>();

            // 1. Fetch ALL data first: actual attendance AND approved leave requests
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id && a.Date >= start && a.Date <= end)
                .ToDictionaryAsync(a => a.Date);

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == employee.Id &&
                             lr.Status == "HR Approved" && // Only show approved leaves
                             lr.StartDate <= end && lr.EndDate >= start)
                .ToListAsync();

            // 2. Loop through every day in the visible calendar range
            for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
            {
                if (day < employee.DateOfJoining.Date || day > DateTime.Today || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue; // Skip days before joining, future days, and weekends
                }

                if (attendances.ContainsKey(day))
                {
                    continue; // A "Present" record exists, so skip. It will be added later.
                }

                // --- THIS IS THE NEW LOGIC ---
                // 3. Check if the user was on approved leave for this day.
                if (leaveRequests.Any(lr => day.Date >= lr.StartDate.Date && day.Date <= lr.EndDate.Date))
                {
                    events.Add(new
                    {
                        title = "On Leave",
                        start = day.ToString("yyyy-MM-dd"),
                        backgroundColor = "#ffc107", // Yellow
                        borderColor = "#ffc107"
                    });
                }
                else
                {
                    // 4. If no attendance AND no leave, they were absent.
                    events.Add(new
                    {
                        title = "Absent",
                        start = day.ToString("yyyy-MM-dd"),
                        backgroundColor = "#dc3545", // Red
                        borderColor = "#dc3545"
                    });
                }
            }

            // 5. Finally, add all the "Present" events.
            events.AddRange(attendances.Values.Select(a => new {
                title = a.Status,
                start = a.Date.ToString("yyyy-MM-dd"),
                backgroundColor = "#198754", // Green
                borderColor = "#198754"
            }));

            return Json(events);
        }

    }
}