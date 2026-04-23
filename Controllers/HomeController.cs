using Microsoft.AspNetCore.Mvc;

namespace The_App.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Users");
            
            return RedirectToAction("Login", "Account");
        }
    }
}
