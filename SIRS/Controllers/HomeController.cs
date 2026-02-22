using Microsoft.AspNetCore.Mvc;

namespace SIRS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
