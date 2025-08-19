using Microsoft.AspNetCore.Mvc;

namespace hrms.Controllers
{
    public class HrController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}