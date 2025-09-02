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
            // ---- NEW LOGIC ----
            // If it's a half day, force the End Date to be the same as the Start Date
            if (viewModel.DurationType.StartsWith("Half Day"))
            {
                viewModel.EndDate = viewModel.StartDate;
            }
            // ---- END NEW LOGIC ----

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
                    EndDate = viewModel.EndDate, // Now using the corrected EndDate
                    Reason = viewModel.Reason,
                    DurationType = viewModel.DurationType // Save the duration type
                };

                var userRole = employee.User?.Role;
                if (userRole is "manager" or "hr")
                {
                    leaveRequest.Status = "Pending HR Approval";
                }
                else
                {
                    leaveRequest.Status = (employee.Department?.HeadOfDepartmentId.HasValue ?? false)
                        ? "Pending Manager Approval"
                        : "Pending HR Approval";
                }

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your leave request has been submitted successfully!";
                return RedirectToAction("Index", "Employee");
            }

            return View(viewModel);
        }
    }
}