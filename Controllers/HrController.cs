using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
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
            var employees = await _context.Employees.ToListAsync();
            return View(employees);
        }

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
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(model);
                }
                if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                await using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var newUser = new User
                        {
                            Username = model.Username,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                            Role = model.Role // Use the role from the form
                        };
                        _context.Users.Add(newUser);
                        await _context.SaveChangesAsync();

                        var newEmployee = new Employee
                        {
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            Department = model.Department,
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
            }
            return View(model);
        }
    }
}