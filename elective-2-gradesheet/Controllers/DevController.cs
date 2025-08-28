using Microsoft.AspNetCore.Mvc;

namespace elective_2_gradesheet.Controllers
{
    public class DevController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
