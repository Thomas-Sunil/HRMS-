using hrms.Data;
using hrms.Models;
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

        // --- CORE HELPER METHOD ---
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
            if (manager == null)
            {
                ViewBag.ErrorMessage = "Could not find a valid employee profile for the logged-in manager.";
                return View(new List<Employee>());
            }

            // ---- START: NEW LOGIC ----
            // Fetch the logged-in Manager's own leave requests
            ViewBag.MyLeaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == manager.Id)
                .OrderByDescending(lr => lr.RequestDate)
                .Take(5)
                .ToListAsync();
            // ---- END: NEW LOGIC ----

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
            var projects = await _context.Projects
                .Where(p => p.ManagerId == manager.Id)
                .Include(p => p.AssignedEmployees)
                .ToListAsync();
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
                Status = "Not Started"
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Projects));
        }

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


        // --- LEAVE APPROVALS ---
        public async Task<IActionResult> LeaveApprovals()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null) return View(new List<LeaveRequest>());
            var pendingRequests = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .Where(lr => lr.Employee.DepartmentId == manager.DepartmentId && lr.Status == "Pending Manager Approval")
                .OrderBy(lr => lr.RequestDate)
                .ToListAsync();
            return View(pendingRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var manager = await GetCurrentManagerAsync();
            var leaveRequest = await _context.LeaveRequests.Include(lr => lr.Employee).FirstOrDefaultAsync(lr => lr.Id == id);
            if (manager == null || leaveRequest == null || leaveRequest.Employee.DepartmentId != manager.DepartmentId)
            {
                return Unauthorized();
            }
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
            if (manager == null || leaveRequest == null || leaveRequest.Employee.DepartmentId != manager.DepartmentId)
            {
                return Unauthorized();
            }
            leaveRequest.Status = "Manager Rejected";
            leaveRequest.ManagerApprovedById = manager.Id;
            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(LeaveApprovals));
        }
        public async Task<IActionResult> OnLeave()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager?.DepartmentId == null)
            {
                // Safety check
                return View(new List<LeaveRequest>());
            }

            var today = DateTime.Today;

            // Find approved leave requests from the manager's team that are active today
            var employeesOnLeave = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .Where(lr => lr.Employee.DepartmentId == manager.DepartmentId &&
                              lr.Status == "HR Approved" &&
                              lr.StartDate.Date <= today &&
                              lr.EndDate.Date >= today)
                .OrderBy(lr => lr.Employee.FirstName)
                .ToListAsync();

            return View(employeesOnLeave);
        }
    }
}