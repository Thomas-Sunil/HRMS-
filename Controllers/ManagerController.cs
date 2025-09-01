using hrms.Data;
using hrms.Models;
//using hrms.ViewModels; // Using hrms.Models now, can be removed if you moved all ViewModels
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    [Authorize(Roles = "manager,hr")]
    public class ManagerController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        private async Task<Employee?> GetCurrentManagerAsync()
        {
            var username = HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;
            return await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        // --- TEAM MANAGEMENT ---
        public async Task<IActionResult> Index()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) { return View("Error", "Could not find a valid employee profile for the logged-in manager."); }
            if (manager.DepartmentId == null)
            {
                ViewBag.ErrorMessage = "You are not assigned to a department. Please contact HR.";
                return View(new List<Employee>());
            }
            ViewBag.MyLeaveRequests = await _context.LeaveRequests.Where(lr => lr.EmployeeId == manager.Id).OrderByDescending(lr => lr.RequestDate).Take(5).ToListAsync();
            var teamMembers = await _context.Employees.Where(e => e.DepartmentId == manager.DepartmentId).Include(e => e.Projects).ToListAsync();
            return View(teamMembers ?? new List<Employee>());
        }

        public async Task<IActionResult> Assign()
        {
            var unassignedEmployees = await _context.Employees.Where(e => e.DepartmentId == null).ToListAsync();
            return View(unassignedEmployees ?? new List<Employee>());
        }

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

        // --- PROJECT MANAGEMENT ---
        public async Task<IActionResult> Projects()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return NotFound();
            var projects = await _context.Projects.Where(p => p.ManagerId == manager.Id).Include(p => p.AssignedEmployees).ToListAsync();
            return View(projects ?? new List<Project>());
        }

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
                Status = "Assigned"
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Projects));
        }

        public async Task<IActionResult> ProjectDetails(int id)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) return Unauthorized();
            var project = await _context.Projects.Include(p => p.AssignedEmployees).Include(p => p.ProjectTasks).ThenInclude(t => t.AssignedEmployee).FirstOrDefaultAsync(p => p.Id == id && p.ManagerId == manager.Id);
            if (project == null) return NotFound();
            var assignedIds = project.AssignedEmployees.Select(e => e.Id).ToList();
            var availableMembers = await _context.Employees.Where(e => e.DepartmentId == manager.DepartmentId && !assignedIds.Contains(e.Id)).ToListAsync();
            var viewModel = new ProjectDetailViewModel
            {
                Project = project,
                AssignedTeamMembers = project.AssignedEmployees,
                AvailableTeamMembers = availableMembers
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTaskToProject(int projectId, string description, int assignedEmployeeId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (!string.IsNullOrEmpty(description) && assignedEmployeeId > 0)
            {
                _context.ProjectTasks.Add(new ProjectTask { ProjectId = projectId, Description = description, AssignedEmployeeId = assignedEmployeeId });
                if (project.Status == "Assigned") { project.Status = "In Progress"; }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ProjectDetails), new { id = projectId });
        }

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

        [HttpGet]
        public async Task<IActionResult> FinalReview(int projectId)
        {
            var project = await _context.Projects.Include(p => p.AssignedEmployees).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();
            var viewModel = new FinalReviewViewModel { Project = project };
            foreach (var employee in project.AssignedEmployees)
            {
                viewModel.Reviews.Add(new PerformanceReviewItem { EmployeeId = employee.Id, EmployeeName = employee.FullName, Rating = 5 });
            }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFinalReviews(FinalReviewViewModel viewModel)
        {
            var manager = await GetCurrentManagerAsync();
            var project = await _context.Projects.Include(p => p.AssignedEmployees).FirstOrDefaultAsync(p => p.Id == viewModel.Project.Id);
            if (project == null || manager == null) return NotFound();
            if (ModelState.IsValid)
            {
                foreach (var reviewItem in viewModel.Reviews)
                {
                    _context.PerformanceReviews.Add(new PerformanceReview { ProjectId = project.Id, EmployeeId = reviewItem.EmployeeId, ManagerId = manager.Id, Rating = reviewItem.Rating, Feedback = reviewItem.Feedback, ReviewDate = DateTime.Today });
                }
                project.Status = "Completed";
                project.AssignedEmployees.Clear();
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Project completed and feedback submitted.";
                return RedirectToAction(nameof(Projects));
            }
            return View("FinalReview", viewModel);
        }

        // --- LEAVE & ATTENDANCE ---
        public async Task<IActionResult> LeaveApprovals()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) return View(new List<LeaveRequest>());
            var pendingRequests = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .Where(lr => lr.Employee.DepartmentId == manager.DepartmentId && lr.Status == "Pending Manager Approval")
                .OrderBy(lr => lr.RequestDate).ToListAsync();
            return View(pendingRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var manager = await GetCurrentManagerAsync();
            var leaveRequest = await _context.LeaveRequests.Include(lr => lr.Employee).FirstOrDefaultAsync(lr => lr.Id == id);
            if (manager == null || leaveRequest == null || leaveRequest.Employee.DepartmentId != manager.DepartmentId) { return Unauthorized(); }
            leaveRequest.Status = "Pending HR Approval";
            leaveRequest.ManagerApprovedById = manager.Id;
            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(LeaveApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var manager = await GetCurrentManagerAsync();
            var leaveRequest = await _context.LeaveRequests.Include(lr => lr.Employee).FirstOrDefaultAsync(lr => lr.Id == id);
            if (manager == null || leaveRequest == null || leaveRequest.Employee.DepartmentId != manager.DepartmentId) { return Unauthorized(); }
            leaveRequest.Status = "Manager Rejected";
            leaveRequest.ManagerApprovedById = manager.Id;
            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(LeaveApprovals));
        }

        public async Task<IActionResult> OnLeave()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) { return View(new List<LeaveRequest>()); }
            var today = DateTime.Today;
            var employeesOnLeave = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .Where(lr => lr.Employee.DepartmentId == manager.DepartmentId && lr.Status == "HR Approved" && lr.StartDate.Date <= today && lr.EndDate.Date >= today)
                .OrderBy(lr => lr.Employee.FirstName).ToListAsync();
            return View(employeesOnLeave);
        }

        public async Task<IActionResult> TeamAttendance()
        {
            var manager = await GetCurrentManagerAsync();
            ViewBag.DepartmentName = manager?.Department?.Name ?? "Your Team";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTeamAttendanceData(DateTime start, DateTime end, int? employeeId = null)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) return Unauthorized();
            var teamMemberQuery = _context.Employees.Where(e => e.DepartmentId == manager.DepartmentId);
            if (employeeId.HasValue) { teamMemberQuery = teamMemberQuery.Where(e => e.Id == employeeId.Value); }
            var teamMembers = await teamMemberQuery.ToListAsync();
            var teamMemberIds = teamMembers.Select(tm => tm.Id).ToList();
            if (!teamMemberIds.Any()) { return Json(new List<object>()); }
            var attendances = await _context.Attendances.Where(a => teamMemberIds.Contains(a.EmployeeId) && a.Date >= start && a.Date <= end).ToDictionaryAsync(a => (a.EmployeeId, a.Date));
            var leaveRequests = await _context.LeaveRequests.Where(lr => teamMemberIds.Contains(lr.EmployeeId) && lr.Status.Contains("Approved") && lr.StartDate.Date <= end.Date && lr.EndDate.Date >= start.Date).ToListAsync();
            var events = new List<object>();
            foreach (var employee in teamMembers)
            {
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    if (day < employee.DateOfJoining.Date || day > DateTime.Today || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) { continue; }
                    if (attendances.ContainsKey((employee.Id, day))) { continue; }
                    if (leaveRequests.Any(lr => lr.EmployeeId == employee.Id && day >= lr.StartDate.Date && day <= lr.EndDate.Date))
                    {
                        events.Add(new { title = employeeId.HasValue ? "On Leave" : $"{employee.FullName} - On Leave", start = day.ToString("yyyy-MM-dd"), backgroundColor = "#ffc107", borderColor = "#ffc107" });
                    }
                    else
                    {
                        events.Add(new { title = employeeId.HasValue ? "Absent" : $"{employee.FullName} - Absent", start = day.ToString("yyyy-MM-dd"), backgroundColor = "#dc3545", borderColor = "#dc3545" });
                    }
                }
            }
            events.AddRange(attendances.Values.Select(a => new { title = employeeId.HasValue ? a.Status : $"{teamMembers.FirstOrDefault(e => e.Id == a.EmployeeId)?.FullName} - {a.Status}", start = a.Date.ToString("yyyy-MM-dd"), backgroundColor = a.Status == "Present" ? "#0d6efd" : "#6c757d", borderColor = a.Status == "Present" ? "#0d6efd" : "#6c757d" }));
            return Json(events);
        }
    }
}