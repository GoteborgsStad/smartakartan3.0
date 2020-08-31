using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartMap.Web.Controllers;
using SmartMap.Web.Util;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Util;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.Controllers
{
    public class FullPageController : BaseController<FullPageController>
    {
        private readonly ICmsApiProxy _cmsApiProxy;
        private readonly string _cmsDomain;

        public FullPageController(ILogger<FullPageController> logger, ICmsApiProxy cmsApiProxy, IConfiguration configuration) : base(logger)
        {
            _cmsApiProxy = cmsApiProxy;
            _cmsDomain = configuration["web-cms:base-url"];
        }

        public async Task<IActionResult> Index()
        {
            var page = await _cmsApiProxy.GetPage(_pageId, _regionValue.PagesApiUrl);

            var htmlBody = StringHelper.SanitizeHtml(page.Content?.Rendered, _cmsDomain);

            var model = new FullPageViewModel
            {
                Header = page.Title.Rendered,
                HtmlBody = htmlBody,
                Ogp = new OgpViewModel
                {
                    Title = page.Title.Rendered,
                    Description = page.Title.Rendered,
                    Type = "website",
                    Url = Request.GetDisplayUrl()
                }
            };

            return View(model);
        }
    }
}