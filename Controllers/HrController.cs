using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    // You can add [Authorize(Roles="hr")] here to protect the whole controller
    public class HrController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Projects) // Include project data for status display
                .ToListAsync();
            return View(employees);
        }

        public async Task<IActionResult> Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Username and email checks
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower()))
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(model);
                }
                if (await _context.Employees.AnyAsync(e => e.Email.ToLower() == model.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // Transaction to create user and employee
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var newUser = new User
                    {
                        Username = model.Username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        Role = model.Role
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    var newEmployee = new Employee
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Position = model.Position,
                        DateOfJoining = model.DateOfJoining,
                        UserId = newUser.Id
                    };
                    _context.Employees.Add(newEmployee);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var viewModel = new EditEmployeeViewModel
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                DepartmentId = employee.DepartmentId,
                Position = employee.Position
            };

            await PopulateDepartmentsViewBag(employee.DepartmentId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var employeeToUpdate = await _context.Employees.FindAsync(id);
                if (employeeToUpdate == null) return NotFound();

                employeeToUpdate.FirstName = viewModel.FirstName;
                employeeToUpdate.LastName = viewModel.LastName;
                employeeToUpdate.Email = viewModel.Email;
                employeeToUpdate.PhoneNumber = viewModel.PhoneNumber;
                employeeToUpdate.DepartmentId = viewModel.DepartmentId;
                employeeToUpdate.Position = viewModel.Position;

                _context.Update(employeeToUpdate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDepartmentsViewBag(viewModel.DepartmentId);
            return View(viewModel);
        }

        // This new action handles unassigning an employee from a department.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignFromDepartment(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.DepartmentId = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDepartmentsViewBag(object selectedDepartment = null)
        {
            var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Departments = new SelectList(departments, "Id", "Name", selectedDepartment);
        }
        public IActionResult CompanyAttendance()
        {
            // This action just returns the view. The calendar fetches data itself.
            return View();
        }

        // GET: /Hr/GetCompanyAttendanceData
        [HttpGet]
        public async Task<IActionResult> GetCompanyAttendanceData(DateTime start, DateTime end)
        {
            var events = new List<object>();

            // 1. Get ALL employees, because HR can see everyone.
            var allEmployees = await _context.Employees.ToListAsync();
            var allEmployeeIds = allEmployees.Select(e => e.Id).ToList();

            if (!allEmployeeIds.Any())
            {
                return Json(new List<object>()); // Return empty if no employees exist
            }

            // 2. Fetch all attendance and leave records in the requested date range
            var attendances = await _context.Attendances
                .Where(a => allEmployeeIds.Contains(a.EmployeeId) && a.Date >= start && a.Date <= end)
                .Include(a => a.Employee) // Must include Employee to get the FullName
                .ToListAsync();

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => allEmployeeIds.Contains(lr.EmployeeId)
                             && lr.Status.Contains("Approved")
                             && lr.StartDate.Date <= end.Date
                             && lr.EndDate.Date >= start.Date)
                .ToListAsync();

            // 3. Loop through all employees and all days to generate Absent/On Leave events
            foreach (var employee in allEmployees)
            {
                for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
                {
                    // Basic validation
                    if (day < employee.DateOfJoining.Date || day > DateTime.Today || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }

                    bool hasAttended = attendances.Any(a => a.EmployeeId == employee.Id && a.Date == day);
                    if (hasAttended) continue; // Skip if a "Present" record exists

                    bool onLeave = leaveRequests.Any(lr => lr.EmployeeId == employee.Id && day >= lr.StartDate.Date && day <= lr.EndDate.Date);

                    if (onLeave)
                    {
                        events.Add(new
                        {
                            title = $"{employee.FullName} - On Leave",
                            start = day.ToString("yyyy-MM-dd"),
                            backgroundColor = "#ffc107", // Yellow
                            borderColor = "#ffc107"
                        });
                    }
                    else
                    {
                        // If no attendance and no leave, employee was absent
                        events.Add(new
                        {
                            title = $"{employee.FullName} - Absent",
                            start = day.ToString("yyyy-MM-dd"),
                            backgroundColor = "#dc3545", // Red
                            borderColor = "#dc3545"
                        });
                    }
                }
            }

            // 4. Add all the "Present" events from the records we fetched
            events.AddRange(attendances.Select(a => new {
                title = $"{a.Employee.FullName} - Present",
                start = a.Date.ToString("yyyy-MM-dd"),
                backgroundColor = "#0d6efd", // Blue
                borderColor = "#0d6efd"
            }));

            return Json(events);
        }
        [HttpGet]
        public async Task<IActionResult> GetEmployeeAttendanceData(int employeeId, DateTime start, DateTime end)
        {
            // Find the specific employee requested
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return Unauthorized();

            var events = new List<object>();

            // Fetch actual attendance and approved leave records
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date <= end)
                .ToDictionaryAsync(a => a.Date);

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == employeeId && lr.Status.Contains("Approved") &&
                             lr.StartDate <= end && lr.EndDate >= start)
                .ToListAsync();

            // Loop through all relevant days to generate events
            for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
            {
                if (day < employee.DateOfJoining.Date || day > DateTime.Today || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                if (attendances.ContainsKey(day))
                {
                    // "Present" event is already handled below
                    continue;
                }

                if (leaveRequests.Any(lr => day >= lr.StartDate.Date && day <= lr.EndDate.Date))
                {
                    events.Add(new { title = "On Leave", start = day.ToString("yyyy-MM-dd"), backgroundColor = "#ffc107", borderColor = "#ffc107" });
                }
                else
                {
                    events.Add(new { title = "Absent", start = day.ToString("yyyy-MM-dd"), backgroundColor = "#dc3545", borderColor = "#dc3545" });
                }
            }

            // Add the "Present" events
            events.AddRange(attendances.Values.Select(a => new {
                title = a.Status, // "Present"
                start = a.Date.ToString("yyyy-MM-dd"),
                backgroundColor = "#198754", // Green
                borderColor = "#198754"
            }));

            return Json(events);
        }
    }
}
    
