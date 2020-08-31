using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Configuration;
using SmartMap.Web.Controllers;
using SmartMap.Web.Util;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Util;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.Controllers
{
    public class HomeController : BaseController<HomeController>
    {
        private readonly ICmsApiProxy _cmsApiProxy;
        private readonly IBusinessRepository _businessRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly IAppCache _cache;
        private readonly string _cmsDomain;

        public HomeController(ILogger<HomeController> logger, 
            ICmsApiProxy cmsApiProxy, 
            IBusinessRepository businessRepository, 
            IRegionRepository regionRepository,
            IConfiguration configuration, 
            IAppCache cache) : base(logger)
        {
            _cmsApiProxy = cmsApiProxy;
            _businessRepository = businessRepository;
            _regionRepository = regionRepository;
            _cache = cache;
            _cmsDomain = configuration["web-cms:base-url"];
        }

        public async Task<IActionResult> Index()
        {
            var regions = await _regionRepository.GetByLanguageCode(_language);

            var region = _regionValue == null 
                ? regions.SingleOrDefault(r => r.PagesApiPath == CmsVariable.DefaultPageApiPath) 
                : regions.SingleOrDefault(r => r.UrlPath == _regionValue.Region);

            var welcomeContent = StringHelper.SanitizeHtml(region?.WelcomeMessage, _cmsDomain);
            var model = new HomeViewModel
            {
                WelcomeContent = welcomeContent,
                Ogp = new OgpViewModel
                {
                    Title = CmsVariable.SiteName,
                    Url = Request.GetDisplayUrl(),
                    Description = StringHelper.StripHtml(welcomeContent),
                    Type = "website",
                    //ImageUrl = 
                }
            };
            return View(model);
        }

        public IActionResult RobotsTxt()
        {
            Response.ContentType = "text/plain";

            return View(new RobotsViewModel
            {
                Host = Request.Host.ToString()
            });
        }

        public async Task<IActionResult> SitemapXml()
        {
            Response.ContentType = "text/xml";

            //var host = Request.Scheme + "://" + Request.Host;
            var host = "https://www.smartakartan.se";
            var cacheKey = "sk-sitemap";

            var model = await _cache.GetOrAddAsync(
                cacheKey,
                () => GetSiteMapXml(host),
                CacheVariable.CacheSiteMapSlidingExpiration);

            return View(model);
        }

        private async Task<UrlViewModel> GetSiteMapXml(string host)
        {
            var model = new UrlViewModel();
            var regions = await _regionRepository.GetAll();

            var languages = await _cmsApiProxy.GetLanguages();
            var defaultLanguage = languages.FirstOrDefault(x => x.Default)?.Code;

            foreach (var region in regions.OrderBy(x => x.MenuOrder))
            {
                var languageCode = region.LanguageCode;
                var langUrl = GetLanguageUrl(languageCode, defaultLanguage);
                var regionPagesUrl = region.PagesApiPath ?? CmsVariable.DefaultPageApiPath;
                var pages = await _cmsApiProxy.GetPages(languageCode, regionPagesUrl);
                var regionSlug = string.IsNullOrEmpty(region.UrlPath) || region.UrlPath == CmsVariable.GlobalUrlPath ? "" : $"/{region.UrlPath}";

                model.Urls.Add(new UrlViewModel.UrlItem { Url = $"{host}{langUrl}{regionSlug}", LastUpdated = region.Modified });

                foreach (var page in pages)
                {
                    var lastUpdated = page.Modified;
                    var url = $"{host}{langUrl}{regionSlug}/{page.Slug}";

                    model.Urls.Add(new UrlViewModel.UrlItem { Url = url, LastUpdated = lastUpdated });
                }
            }

            var allBusinesses = await _businessRepository.GetAll();
            foreach (var business in allBusinesses)
            {
                var lastUpdated = business.LastUpdated.ToString(); // "yyyy-MM-dd"
                var url = $"{host}{business.DetailPageLink}";

                model.Urls.Add(new UrlViewModel.UrlItem { Url = url, LastUpdated = lastUpdated });
            }

            return model;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetLanguageUrl(string languageCode, string defaultLanguage)
        {
            return languageCode.Contains(defaultLanguage) ? "" : $"/{languageCode}";
        }
    }

    public class UrlViewModel
    {
        public IList<UrlItem> Urls { get; set; } = new List<UrlItem>();

        public class UrlItem
        {
            public string Url { get; set; }
            public string LastUpdated { get; set; }
        }
    }


    public class RobotsViewModel
    {
        public string Host { get; set; }
    }
}
