using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartMap.Web.Controllers;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.Controllers
{
    public class CalendarController : BaseController<CalendarController>
    {
        private readonly ICmsApiProxy _cmsApiProxy;

        public CalendarController(ILogger<CalendarController> logger, ICmsApiProxy cmsApiProxy) : base(logger)
        {
            _cmsApiProxy = cmsApiProxy;
        }

        public async Task<IActionResult> Index()
        {
            var page = await _cmsApiProxy.GetPage(_pageId, _regionValue.PagesApiUrl);

            var model = new CalendarViewModel
            {
                Title = page.Title.Rendered
            };

            return View(model);
        }
    }
}