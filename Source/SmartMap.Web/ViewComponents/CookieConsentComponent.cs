using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.ViewComponents
{
    [ViewComponent(Name = "CookieConsent")]
    public class CookieConsentComponent : ViewComponent
    {
        private readonly ICmsApiProxy _cmsApiProxy;

        public CookieConsentComponent(ICmsApiProxy cmsApiProxy)
        {
            _cmsApiProxy = cmsApiProxy;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var language = RouteData?.Values["language"]?.ToString();
            var show = !(HttpContext.Request.Cookies["cookieconsent"] != null && HttpContext.Request.Cookies["cookieconsent"] == "1");

            var model = new CookieConsentViewModel
            {
                Translations = await _cmsApiProxy.GetTranslationsByPrefix(language, "cookieconsent."),
                Context = HttpContext,
                ShowCookieConsent = show
            };

            return View(model);
        }
    }
}
