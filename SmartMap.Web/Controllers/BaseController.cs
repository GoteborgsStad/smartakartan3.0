using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SmartMap.Web.Routers;
using SmartMap.Web.Util;

namespace SmartMap.Web.Controllers
{
    public class BaseController<T> : Controller
    {
        protected readonly ILogger<T> _logger;

        protected string _language;
        protected int _pageId;
        protected RegionValue _regionValue;

        public BaseController(ILogger<T> logger)
        {
            _logger = logger;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.RouteData != null)
            {
                _language = context.RouteData.Values["language"]?.ToString();
                int.TryParse(context.RouteData.Values["pageId"]?.ToString(), out _pageId);

                _regionValue = context.RouteData.Values["regionValue"] != null 
                    ? context.RouteData.Values["regionValue"] as RegionValue
                    : null;

                var languageCode = string.IsNullOrEmpty(_language) ? CmsVariable.DefaultLanguageCode : _language;;
                ViewData["languageCode"] = languageCode;
                ViewData["languageTerritory"] = LanguageTerritory(languageCode);
            }
            base.OnActionExecuting(context);
        }

        private static string LanguageTerritory(string languageCode)
        {
            return languageCode switch
            {
                "sv" => $"{languageCode}_SE",
                "en" => $"{languageCode}_US",
                _ => languageCode
            };
        }
    }
}