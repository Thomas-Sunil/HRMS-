using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace hrms.ViewComponents
{
    public class AttendanceViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public AttendanceViewComponent(ApplicationDbContext context) { _context = context; }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity is not ClaimsIdentity claimsIdentity || !claimsIdentity.IsAuthenticated)
            {
                return Content(string.Empty);
            }

            var username = claimsIdentity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return Content(string.Empty);

            var today = DateTime.Today;
            var attendanceRecord = await _context.Attendances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

            // Default state: Can clock in, cannot clock out.
            var viewModel = new AttendanceWidgetViewModel
            {
                CanClockIn = true,
                CanClockOut = false,
                IsCompleted = false,
                ClockInTime = null
            };

            if (attendanceRecord != null)
            {
                viewModel.ClockInTime = attendanceRecord.ClockInTime;

                if (attendanceRecord.ClockInTime != null && attendanceRecord.ClockOutTime == null)
                {
                    // User has clocked in, but not out.
                    viewModel.CanClockIn = false;
                    viewModel.CanClockOut = true;
                }
                else if (attendanceRecord.ClockInTime != null && attendanceRecord.ClockOutTime != null)
                {
                    // User has completed attendance for the day.
                    viewModel.CanClockIn = false;
                    viewModel.CanClockOut = false;
                    viewModel.IsCompleted = true;
                }
            }

            return View(viewModel);
        }
    }
}