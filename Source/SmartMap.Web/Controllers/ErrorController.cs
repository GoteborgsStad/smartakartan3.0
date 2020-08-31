using Microsoft.AspNetCore.Mvc;

namespace SmartMap.Web.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Error404()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}