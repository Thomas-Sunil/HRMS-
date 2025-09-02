using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    [Authorize]
    public class LeaveController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // --- THIS IS THE FULLY IMPLEMENTED HELPER METHOD ---
        private async Task<Employee?> GetCurrentUserEmployeeAsync()
        {
            var username = HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            return await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        // --- THE REST OF THE CONTROLLER IS NOW CORRECT ---

        // GET: /Leave/Apply
        public IActionResult Apply()
        {
            var viewModel = new LeaveRequestViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequestViewModel viewModel)
        {
            // First, force EndDate to match StartDate if it's a half-day leave
            if (viewModel.DurationType.StartsWith("Half Day"))
            {
                viewModel.EndDate = viewModel.StartDate;
            }

            // Re-check model validity after our adjustment
            ModelState.Clear(); // Clear previous errors, if any
            TryValidateModel(viewModel); // Re-run validation

            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null) return Unauthorized();

            if (ModelState.IsValid)
            {
                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = employee.Id,
                    RequestDate = DateTime.Today,
                    LeaveType = viewModel.LeaveType,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    Reason = viewModel.Reason,
                    DurationType = viewModel.DurationType
                };

                // --- THIS IS THE NEW, ENHANCED WORKFLOW LOGIC ---
                var userRole = employee.User?.Role;

                if (userRole == "hr")
                {
                    // An HR person's leave request logic:
                    if (employee.ReportingHrId.HasValue && employee.ReportingHrId != employee.Id)
                    {
                        // If they have a Reporting HR (who is not themselves), it goes to them.
                        // We still call this "Pending HR Approval" because the approver is also an HR person.
                        leaveRequest.Status = "Pending HR Approval";
                    }
                    else
                    {
                        // If they are their own Reporting HR or have none, it's auto-approved.
                        leaveRequest.Status = "HR Approved";
                        leaveRequest.HrApprovedById = employee.Id; // Record self-approval
                    }
                }
                else if (userRole == "manager")
                {
                    // A Manager's leave request goes directly to HR for approval.
                    leaveRequest.Status = "Pending HR Approval";
                }
                else // This is a regular "employee"
                {
                    // An Employee's request goes to their manager, if one exists.
                    var departmentHeadId = employee.Department?.HeadOfDepartmentId;
                    if (departmentHeadId.HasValue)
                    {
                        leaveRequest.Status = "Pending Manager Approval";
                    }
                    else
                    {
                        // If their dept has no manager, escalate to HR.
                        leaveRequest.Status = "Pending HR Approval";
                    }
                }

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your leave request was submitted successfully!";
                return RedirectToAction("Index", "Employee");
            }

            return View(viewModel);
        }
    }
}