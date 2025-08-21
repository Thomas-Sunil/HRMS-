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
    public class AttendanceController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        private async Task<Employee> GetCurrentUserEmployeeAsync()
        {
            var username = HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockIn()
        {
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null) return Unauthorized();

            var today = DateTime.Today;

            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

            if (existingAttendance == null)
            {
                var newAttendance = new Attendance
                {
                    EmployeeId = employee.Id,
                    Date = today,
                    ClockInTime = DateTime.Now.TimeOfDay,
                    Status = "Present"
                };
                _context.Attendances.Add(newAttendance);
            }
            // This case handles an edge case where a record exists but is missing the clock-in time
            else if (existingAttendance.ClockInTime == null)
            {
                existingAttendance.ClockInTime = DateTime.Now.TimeOfDay;
                existingAttendance.Status = "Present";
                _context.Attendances.Update(existingAttendance);
            }

            await _context.SaveChangesAsync();
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockOut()
        {
            var employee = await GetCurrentUserEmployeeAsync();
            if (employee == null) return Unauthorized();

            var today = DateTime.Today;

            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

            // We can only clock out if we have already clocked in
            if (existingAttendance != null && existingAttendance.ClockInTime != null && existingAttendance.ClockOutTime == null)
            {
                existingAttendance.ClockOutTime = DateTime.Now.TimeOfDay;
                _context.Attendances.Update(existingAttendance);
                await _context.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}