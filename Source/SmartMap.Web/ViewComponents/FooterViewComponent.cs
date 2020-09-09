using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;
using SmartMap.Web.Routers;
using SmartMap.Web.Util;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.ViewComponents
{
    [ViewComponent(Name = "Footer")]
    public class FooterViewComponent : ViewComponent
    {
        private readonly ICmsApiProxy _cmsApiProxy;

        public FooterViewComponent(ICmsApiProxy cmsApiProxy)
        {
            _cmsApiProxy = cmsApiProxy;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var language = RouteData?.Values["language"]?.ToString();

            var regionValue = RouteData?.Values["regionValue"] as RegionValue;
            var region = regionValue?.Region;
            var (regionPagesUrl, _, regionTitle, _) = await _cmsApiProxy.GetRegionList(region, language);

            var globalPages = await _cmsApiProxy.GetPages(language);

            var urlList = new List<string>();
            var partialUrl = string.Empty;

            if (!string.IsNullOrEmpty(language))
                urlList.Add(language);

            if (urlList.Any())
                partialUrl = string.Join("/", urlList);

            var leftFooterPages = GetPages(globalPages, CmsPageType.LeftFooter, partialUrl);
            var centerFooterPages = GetPages(globalPages, CmsPageType.CenterFooter, partialUrl);
            var rightFooterPages = GetPages(globalPages, CmsPageType.RightFooter, partialUrl);

            if (regionPagesUrl != CmsVariable.DefaultPageApiPath)
            {
                urlList = new List<string>();

                if (!string.IsNullOrEmpty(language))
                    urlList.Add(language);

                if (!string.IsNullOrEmpty(region))
                    urlList.Add(region);

                if (urlList.Any())
                    partialUrl = string.Join("/", urlList);

                var pages = await _cmsApiProxy.GetPages(language, regionPagesUrl);
                leftFooterPages = GetPages(pages, CmsPageType.LeftFooter, partialUrl);
            }

            var translations = await _cmsApiProxy.GetTranslationsByPrefix(language, "footer.");

            var model = new FooterViewModel
            {
                Translations = translations,
                Header = regionTitle == "" ? translations["footer.center-title"] : CmsVariable.SiteName,
                RegionTitle = regionTitle == "" ? CmsVariable.SiteName : regionTitle,
                LeftFooterPages = leftFooterPages,
                CenterFooterPages = centerFooterPages,
                RightFooterPages = rightFooterPages,
                Languages = new List<FooterViewModel.LanguageViewModel>()
            };

            // TODO: Keep current route (region, page) when switching language?
            var languages = await _cmsApiProxy.GetLanguages();
            foreach (var l in languages)
            {
                model.Languages.Add(l.Default
                    ? new FooterViewModel.LanguageViewModel("/", l.Name)
                    : new FooterViewModel.LanguageViewModel($"/{l.Code}/", l.Name));
            }

            return View(model);
        }

        private List<FooterViewModel.PageViewModel> GetPages(IList<PageCmsModel> pages, CmsPageType pageType, string partialUrl)
        {
            return pages
                .Where(p =>
                    p.Page_type?.First().TypeName == Enum.GetName(typeof(CmsPageType), pageType))
                .Select(n => new FooterViewModel.PageViewModel
                {
                    Id = n.Id,
                    PageName = n.Title?.Rendered,
                    UrlPath = !string.IsNullOrEmpty(partialUrl) ? $"/{partialUrl}/{n.Slug}" : $"/{n.Slug}"
                }).ToList();
        }
    }
}
