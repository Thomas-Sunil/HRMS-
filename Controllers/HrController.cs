using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    [Authorize(Roles = "hr")]
    public class HrController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HrController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- DASHBOARD & EMPLOYEE MANAGEMENT ---
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Projects)
                .OrderBy(e => e.FirstName)
                .ToListAsync();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());
            if (currentUser != null)
            {
                var hrEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUser.Id);
                if (hrEmployee != null)
                {
                    ViewBag.MyLeaveRequests = await _context.LeaveRequests
                        .Where(lr => lr.EmployeeId == hrEmployee.Id)
                        .OrderByDescending(lr => lr.RequestDate)
                        .Take(5)
                        .ToListAsync();
                }
            }
            return View(employees);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View(new AddEmployeeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddEmployeeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }
            if (await _context.Employees.AnyAsync(e => e.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Work email already registered.");
                return View(model);
            }

            var newEmployee = new Employee
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Position = model.Position,
                DateOfJoining = model.DateOfJoining,
                AddressLine1 = model.AddressLine1,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country,
                HighestQualification = model.HighestQualification,
                PhoneNumber = model.PhoneNumber
            };

            newEmployee.User = new User
            {
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role
            };

            if (model.Photo != null)
            {
                string folder = Path.Combine("uploads", "photos");
                newEmployee.PhotoPath = await UploadFile(folder, model.Photo);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Employees.Add(newEmployee);
                await _context.SaveChangesAsync();

                if (model.Certificates != null)
                {
                    string certFolder = Path.Combine("uploads", "certificates");
                    foreach (var certificateFile in model.Certificates)
                    {
                        var document = new EmployeeDocument
                        {
                            EmployeeId = newEmployee.Id,
                            DocumentName = Path.GetFileNameWithoutExtension(certificateFile.FileName),
                            FilePath = await UploadFile(certFolder, certificateFile),
                            UploadedAt = DateTime.UtcNow
                        };
                        _context.EmployeeDocuments.Add(document);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Employee created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"An error occurred while saving to the database: {ex.Message}");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // Map all the data from the Employee entity to the ViewModel
            var viewModel = new EditEmployeeViewModel
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                Position = employee.Position,
                AddressLine1 = employee.AddressLine1,
                City = employee.City,
                PostalCode = employee.PostalCode,
                Country = employee.Country,
                HighestQualification = employee.HighestQualification,
                DepartmentId = employee.DepartmentId,
                ReportingHrId = employee.ReportingHrId
            };

            await PopulateEditDropdowns(employee.DepartmentId, employee.ReportingHrId);
            return View(viewModel);
        }

        // POST: /Hr/Edit/5 - NEW, EXPANDED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var employeeToUpdate = await _context.Employees.FindAsync(id);
                if (employeeToUpdate == null) return NotFound();

                // Map all the updated data from the ViewModel back to the entity
                employeeToUpdate.FirstName = viewModel.FirstName;
                employeeToUpdate.LastName = viewModel.LastName;
                employeeToUpdate.Email = viewModel.Email;
                employeeToUpdate.PhoneNumber = viewModel.PhoneNumber;
                employeeToUpdate.Position = viewModel.Position;
                employeeToUpdate.AddressLine1 = viewModel.AddressLine1;
                employeeToUpdate.City = viewModel.City;
                employeeToUpdate.PostalCode = viewModel.PostalCode;
                employeeToUpdate.Country = viewModel.Country;
                employeeToUpdate.HighestQualification = viewModel.HighestQualification;
                employeeToUpdate.DepartmentId = viewModel.DepartmentId;
                employeeToUpdate.ReportingHrId = viewModel.ReportingHrId;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee details updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            // If the model is invalid, we must repopulate the dropdowns and return the view
            await PopulateEditDropdowns(viewModel.DepartmentId, viewModel.ReportingHrId);
            return View(viewModel);
        }

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

        // --- LEAVE & ATTENDANCE ---
        public async Task<IActionResult> LeaveApprovals()
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());
            if (currentUser == null) return Unauthorized();

            var hrEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUser.Id);
            if (hrEmployee == null) return View(new List<LeaveRequest>());

            var statuses = new[] { "Pending HR Approval", "Pending Manager Approval" };
            var requests = await _context.LeaveRequests
                .Include(lr => lr.Employee.Department)
                .Where(lr => lr.EmployeeId != hrEmployee.Id && statuses.Contains(lr.Status))
                .OrderByDescending(lr => lr.RequestDate).ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());
            if (currentUser == null) return Unauthorized();

            var hrEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUser.Id);
            if (hrEmployee == null) return Unauthorized();

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = "HR Approved";
                leaveRequest.HrApprovedById = hrEmployee.Id;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());
            if (currentUser == null) return Unauthorized();

            var hrEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUser.Id);
            if (hrEmployee == null) return Unauthorized();

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = "HR Rejected";
                leaveRequest.HrApprovedById = hrEmployee.Id;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        public async Task<IActionResult> OnLeave()
        {
            var today = DateTime.Today;
            var employeesOnLeave = await _context.LeaveRequests
                .Include(lr => lr.Employee.Department)
                .Where(lr => lr.Status == "HR Approved" && lr.StartDate.Date <= today && lr.EndDate.Date >= today)
                .OrderBy(lr => lr.Employee.FirstName).ToListAsync();
            return View(employeesOnLeave);
        }

        [HttpGet]
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetEmployeeAttendanceData(int employeeId, DateTime start, DateTime end)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return Json(new List<object>());

            var events = new List<object>();
            var attendances = await _context.Attendances.Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date <= end).ToDictionaryAsync(a => a.Date);
            var leaveRequests = await _context.LeaveRequests.Where(lr => lr.EmployeeId == employeeId && lr.Status.Contains("Approved") && lr.StartDate <= end && lr.EndDate >= start).ToListAsync();

            for (var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
            {
                if (day < employee.DateOfJoining.Date || day > DateTime.Today || day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) continue;
                if (attendances.ContainsKey(day)) continue;

                if (leaveRequests.Any(lr => day.Date >= lr.StartDate.Date && day.Date <= lr.EndDate.Date))
                {
                    events.Add(new { title = "On Leave", start = day.ToString("yyyy-MM-dd"), backgroundColor = "#ffc107" });
                }
                else
                {
                    events.Add(new { title = "Absent", start = day.ToString("yyyy-MM-dd"), backgroundColor = "#dc3545" });
                }
            }
            events.AddRange(attendances.Values.Select(a => new { title = a.Status, start = a.Date.ToString("yyyy-MM-dd"), backgroundColor = "#198754" }));

            return Json(events);
        }

        // --- HELPERS ---
        private async Task<string> UploadFile(string folderPath, IFormFile file)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string targetFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, folderPath);
            Directory.CreateDirectory(targetFolderPath);
            string targetFilePath = Path.Combine(targetFolderPath, uniqueFileName);

            await using (var fileStream = new FileStream(targetFilePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return Path.Combine("/", folderPath, uniqueFileName).Replace('\\', '/');
        }

        private async Task PopulateEditDropdowns(object selectedDept = null, object selectedHr = null)
        {
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", selectedDept);
            ViewBag.Hrs = new SelectList(await _context.Employees.Include(e => e.User).Where(e => e.User.Role == "hr").OrderBy(e => e.FirstName).ToListAsync(), "Id", "FullName", selectedHr);
        }
    }
}