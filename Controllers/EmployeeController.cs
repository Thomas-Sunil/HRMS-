using Microsoft.AspNetCore.Mvc;

namespace hrms.Controllers
{
    public class EmployeeController : Controller
    {
        // This action will display the Employee's dashboard page
        public IActionResult Index()
        {
            return View();
        }
    }
}