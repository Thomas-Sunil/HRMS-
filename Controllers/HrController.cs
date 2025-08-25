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
    public class HrController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // --- UNCHANGED METHODS ---
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Projects)
                .ToListAsync();
            return View(employees);
        }

        public IActionResult Add()
        {
            return View();
        }

        // --- UPDATED METHODS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
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
                        // DepartmentId and ReportingHrId are intentionally null on creation
                    };
                    _context.Employees.Add(newEmployee);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception) // No need to use 'ex' if you are not logging it
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "An unexpected error occurred while creating the employee.");
                }
            }
            return View(model);
        }

        // GET Edit - Fully Implemented
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
                Position = employee.Position,
                ReportingHrId = employee.ReportingHrId
            };

            await PopulateEditDropdowns(employee.DepartmentId, employee.ReportingHrId);
            return View(viewModel);
        }

        // POST Edit - Fully Implemented
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
                employeeToUpdate.ReportingHrId = viewModel.ReportingHrId;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateEditDropdowns(viewModel.DepartmentId, viewModel.ReportingHrId);
            return View(viewModel);
        }

        // Unassign Action - Fully Implemented
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
        public async Task<IActionResult> LeaveApprovals()
        {
            var hrUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());

            // Don't show the HR's own leave requests on their approval dashboard
            var hrEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == hrUser.Id);

            var requests = await _context.LeaveRequests
                .Include(lr => lr.Employee).ThenInclude(e => e.Department)
                .Where(lr => lr.EmployeeId != hrEmployee.Id &&
                               (lr.Status == "Pending HR Approval" || lr.Status == "Pending Manager Approval"))
                .OrderByDescending(lr => lr.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        // POST: /Hr/ApproveLeave/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var hrEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.User.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = "HR Approved";
                leaveRequest.HrApprovedById = hrEmployee.Id;
                _context.Update(leaveRequest);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        // POST: /Hr/RejectLeave/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var hrEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.User.Username.ToLower() == HttpContext.User.Identity.Name.ToLower());

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = "HR Rejected";
                leaveRequest.HrApprovedById = hrEmployee.Id;
                _context.Update(leaveRequest);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        // Renamed Helper for Edit Form
        private async Task PopulateEditDropdowns(object selectedDept = null, object selectedHr = null)
        {
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", selectedDept);
            ViewBag.Hrs = new SelectList(await _context.Employees.Include(e => e.User).Where(e => e.User.Role == "hr").OrderBy(e => e.FirstName).ToListAsync(), "Id", "FullName", selectedHr);
        }
    }
}