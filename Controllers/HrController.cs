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

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            var employees = await _context.Employees.Include(e => e.Department).ToListAsync();
            return View(employees);
        }

        // GET action is now very simple.
        public IActionResult Add()
        {
            return View();
        }

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
                        // DepartmentId is now removed from this process. It will be null by default.
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

        // POST: /Hr/Edit/5
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

                try
                {
                    _context.Update(employeeToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency error if needed
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            // If model is invalid, repopulate dropdown and return view
            await PopulateDepartmentsViewBag(viewModel.DepartmentId);
            return View(viewModel);
        }

        // This helper method is no longer needed by the Add actions, but we keep it for other potential uses.
        private async Task PopulateDepartmentsViewBag(object selectedDepartment = null)
        {
            var departments = await _context.Departments
                                            .OrderBy(d => d.Name)
                                            .ToListAsync();
            ViewBag.Departments = new SelectList(departments, "Id", "Name", selectedDepartment);
        }
    }
}