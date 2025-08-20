using hrms.Data;
using hrms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace hrms.Controllers
{
    public class DepartmentController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                                            .Include(d => d.HeadOfDepartment)
                                            .OrderBy(d => d.Name)
                                            .ToListAsync();
            return View(departments);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateManagersViewBag();
            return View(); // Send an empty ViewModel
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var department = new Department
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    HeadOfDepartmentId = viewModel.HeadOfDepartmentId
                };

                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If validation fails, repopulate the ViewBag and return the view
            await PopulateManagersViewBag(viewModel.HeadOfDepartmentId);
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            await PopulateManagersViewBag(department.HeadOfDepartmentId);

            var viewModel = new DepartmentViewModel
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                HeadOfDepartmentId = department.HeadOfDepartmentId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DepartmentViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var departmentToUpdate = await _context.Departments.FindAsync(id);
                    if (departmentToUpdate == null) return NotFound();

                    departmentToUpdate.Name = viewModel.Name;
                    departmentToUpdate.Description = viewModel.Description;
                    departmentToUpdate.HeadOfDepartmentId = viewModel.HeadOfDepartmentId;

                    _context.Update(departmentToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Departments.Any(e => e.Id == viewModel.Id)) { return NotFound(); }
                    else { throw; }
                }
                return RedirectToAction(nameof(Index));
            }

            // If validation fails, repopulate the ViewBag and return the view
            await PopulateManagersViewBag(viewModel.HeadOfDepartmentId);
            return View(viewModel);
        }

        private async Task PopulateManagersViewBag(object selectedValue = null)
        {
            var managers = await _context.Employees
                                         .Include(e => e.User)
                                         .Where(e => e.User.Role == "manager")
                                         .OrderBy(e => e.FirstName)
                                         .ThenBy(e => e.LastName)
                                         .ToListAsync();

            ViewBag.ManagerList = new SelectList(managers, "Id", "FullName", selectedValue);
        }
    }
}