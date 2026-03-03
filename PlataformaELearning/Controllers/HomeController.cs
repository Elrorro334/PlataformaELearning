using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlataformaELearning.Controllers
{
    [Authorize] // Solo alumnos logueados pueden ver el dashboard
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}