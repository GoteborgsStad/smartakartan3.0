using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Routers;
using SmartMap.Web.Util;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.ViewComponents
{
    [ViewComponent(Name = "TopBar")]
    public class TopBarViewComponent : ViewComponent
    {
        private readonly ICmsApiProxy _cmsApiProxy;

        public TopBarViewComponent(ICmsApiProxy cmsApiProxy)
        {
            _cmsApiProxy = cmsApiProxy;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var language = RouteData?.Values["language"]?.ToString();

            var regionValue = RouteData?.Values["regionValue"] as RegionValue;
            var region = regionValue?.Region;
            var (regionPagesUrl, _, regionTitle, regions) = await _cmsApiProxy.GetRegionList(region, language);

            var baseUrl = $"/{language}";

            var model = new TopBarViewModel
            {
                Regions = regions.Select(n => new TopBarViewModel.RegionViewModel
                {
                    Id = n.Id,
                    Name = n.Title?.Rendered,
                    UrlPath = GetRegionUrlPath(language, n.Url_path)
                }).ToList(),
                Region = regionTitle,
                LanguageCode = language,
                BasePartialUrl = baseUrl
            };

            var pages = await _cmsApiProxy.GetPages(language, regionPagesUrl);

            var urlList = new List<string>();
            var partialUrl = string.Empty;

            if (!string.IsNullOrEmpty(language))
                urlList.Add(language);

            if (!string.IsNullOrEmpty(region))
                urlList.Add(region);

            if (urlList.Any())
                partialUrl = string.Join("/", urlList);

            model.RegionUrl = $"/{partialUrl}";

            // Get only top menu pages
            model.Pages = pages
                .Where(p => p.Page_type?.First().TypeName == CmsVariable.TopMenuPageTypeName)
                .Select(n => new TopBarViewModel.PageViewModel
                {
                    Id = n.Id,
                    PageName = n.Title?.Rendered,
                    UrlPath = !string.IsNullOrEmpty(partialUrl) ? $"{partialUrl}/{n.Slug}" : n.Slug
                }).ToList();

            model.RootUrl = $"{Request.Scheme}://{Request.Host}";

            model.Translations = await _cmsApiProxy.GetTranslationsByPrefix(language, "topbar.");

            return View(model);
        }

        private string GetRegionUrlPath(string language, string urlPath)
        {
            if (urlPath == CmsVariable.GlobalUrlPath)
                urlPath = "";

            return !string.IsNullOrEmpty(language) ? $"{language}/{urlPath}" : urlPath;
        }
    }


}
