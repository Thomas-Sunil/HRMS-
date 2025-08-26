using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace hrms.Controllers
{
    [Authorize(Roles = "manager,hr")]
    public class MeetingController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // --- FULLY IMPLEMENTED HELPER ---
        private async Task<Employee?> GetCurrentManagerAsync()
        {
            var username = HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        // --- FULLY IMPLEMENTED ACTIONS ---
        public async Task<IActionResult> Index()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            var meetings = await _context.Meetings
                .Where(m => m.CreatedById == manager.Id)
                .Include(m => m.Invitations)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            return View(meetings);
        }

        public async Task<IActionResult> Create()
        {
            var manager = await GetCurrentManagerAsync();
            var teamMembers = new List<Employee>();

            if (manager?.DepartmentId != null)
            {
                teamMembers = await _context.Employees
                    .Where(e => e.DepartmentId == manager.DepartmentId)
                    .OrderBy(e => e.FirstName).ToListAsync();
            }

            var viewModel = new CreateMeetingViewModel
            {
                AvailableTeamMembers = teamMembers,
                StartTime = DateTime.Now.Date.AddHours(DateTime.Now.Hour + 1),
                EndTime = DateTime.Now.Date.AddHours(DateTime.Now.Hour + 2)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMeetingViewModel viewModel)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            if (viewModel.StartTime >= viewModel.EndTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after the start time.");
            }

            if (ModelState.IsValid)
            {
                var newMeeting = new Meeting
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    StartTime = viewModel.StartTime,
                    EndTime = viewModel.EndTime,
                    CreatedById = manager.Id
                };

                if (viewModel.InvitedEmployeeIds != null)
                {
                    foreach (var employeeId in viewModel.InvitedEmployeeIds)
                    {
                        newMeeting.Invitations.Add(new MeetingInvitation
                        {
                            EmployeeId = employeeId,
                            Status = "Pending"
                        });
                    }
                }

                _context.Meetings.Add(newMeeting);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Meeting created and invitations sent successfully!";
                return RedirectToAction(nameof(Index));
            }

            if (manager.DepartmentId != null)
            {
                viewModel.AvailableTeamMembers = await _context.Employees
                    .Where(e => e.DepartmentId == manager.DepartmentId)
                    .OrderBy(e => e.FirstName)
                    .ToListAsync();
            }

            return View(viewModel);
        }
    }
}