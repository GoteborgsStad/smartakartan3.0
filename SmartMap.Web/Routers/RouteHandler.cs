using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Routers
{
    public class RouteHandler : IRouteHandler
    {
        private readonly string _defaultAction = "Index";
        private readonly string _notFoundAction = "Error404";
        private readonly string _defaultController = "Home";
        private readonly string _errorController = "Error";

        private readonly ICmsApiProxy _cmsApiProxy;
        public readonly IBusinessRepository BusinessRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly ILogger<CmsApiProxy> _logger;

        public RouteHandler(ICmsApiProxy cmsApiProxy, ILogger<CmsApiProxy> logger, IBusinessRepository businessRepository, IRegionRepository regionRepository)
        {
            _cmsApiProxy = cmsApiProxy;
            _logger = logger;
            BusinessRepository = businessRepository;
            _regionRepository = regionRepository;
        }

        public async Task<RouteValueDictionary> GetRouteValue(RouteValueDictionary values)
        {
            var routeType = "Web";
            if (values.ContainsKey("language"))
            {
                routeType = ((string)values["language"]).ToLower() == "api" ? "Api" : "Web";
            }

            var urlPath = string.Join("/", values.Select(p => p.Value).ToArray());
            _logger.LogInformation("[{RouteType}] Requested url path {UrlPath}", routeType, urlPath);

            string
                languageCode = null,
                regionName = null,
                page = null;

            if (values.ContainsKey("language"))
                languageCode = (string)values["language"];

            if (values.ContainsKey("region"))
                regionName = (string)values["region"];

            if (values.ContainsKey("page"))
                page = (string)values["page"];


            if (string.IsNullOrEmpty(languageCode) &&
                string.IsNullOrEmpty(regionName) &&
                string.IsNullOrEmpty(page))
            {
                values["controller"] = _defaultController;
                values["action"] = _defaultAction;

                return values;
            }

            if (languageCode == "robots.txt")
            {
                values["controller"] = "Home";
                values["action"] = "RobotsTxt";
                return values;
            }

            if (languageCode == "sitemap.xml")
            {
                values["controller"] = "Home";
                values["action"] = "SitemapXml";
                return values;
            }

            var languages = _cmsApiProxy.GetLanguages()?.Result?.ToList();
            if (languages != null && !languages.Any(l => l.Code == languageCode))
            {
                page = regionName;
                regionName = languageCode;
                languageCode = null;
            }

            //var regions = _cmsApiProxy.GetRegions(languageCode)?.Result?.ToList();
            var regions = _regionRepository.GetByLanguageCode(languageCode)?.Result?.ToList();
            if (regions != null && !regions.Any(r => r.UrlPath == regionName))
            {
                page = regionName;
                regionName = null;
            }

            var region = regions?.FirstOrDefault(r => r.UrlPath == regionName);
            var regionPagesUrl = region?.PagesApiPath ?? CmsVariable.DefaultPageApiPath;
            var regionBusinessUrl = region?.BusinessesApiPath ?? CmsVariable.DefaultBusinessApiPath;

            // Resolve with controller to hit based on page
            (string controller, int pageId) = await Resolve(languageCode, page, regionName, regions);

            if (controller == null) return values;

            var regionValue = new RegionValue
            {
                Region = regionName,
                PagesApiUrl = regionPagesUrl,
                BusinessApiUrl = regionBusinessUrl
            };

            values["controller"] = controller;
            values["action"] = _defaultAction;
            values["language"] = languageCode;
            values["pageId"] = pageId;
            values["regionValue"] = regionValue;

            if (controller == _errorController)
                values["action"] = _notFoundAction;

            return values;
        }


        private async Task<(string controller, int pageId)> Resolve(
            string language,
            string routePage,
            string routeRegion,
            List<RegionElasticModel> regions)
        {
            if (string.IsNullOrEmpty(routePage))
            {
                return (_defaultController, -1);
            }
            
            var normalizedPage = routePage.ToLowerInvariant();

            var activeRegion = regions?.FirstOrDefault(r => r.UrlPath == routeRegion);
            var regionPagesUrl = activeRegion?.PagesApiPath ?? CmsVariable.DefaultPageApiPath;

            var pages = await _cmsApiProxy.GetPages(language, regionPagesUrl);

            var page = pages.FirstOrDefault(p => p.Slug == normalizedPage);
            var controller = page?.Page_type?.FirstOrDefault()?.Template_name;
            var pageId = page?.Id ?? -1;

            if (controller == null)
            {
                var businessPagePartialUrl = $"/{normalizedPage}";
                var languageCode = string.IsNullOrEmpty(language) ? CmsVariable.DefaultLanguageCode : language;
                var businesses = await BusinessRepository.GetBusinesses(randomSeed: 0, from: 0, size: CmsVariable.ElasticSize, languageCode: languageCode);
                var business = businesses?.Items?.FirstOrDefault(b => b.Business.DetailPageLink.EndsWith(businessPagePartialUrl));
                if (business?.Business != null)
                {
                    _logger.LogInformation("Found business from {PageUrl} with language code {LanguageCode} and region {RouteRegion}. Business language code {BusinessLanguageCode}, city id:{Region}, url:{Url}.",
                        businessPagePartialUrl, languageCode, routeRegion, business.Business.LanguageCode, business.Business.City.Name, business.Business.DetailPageLink);
                    pageId = business.Business.Id;
                    var templateName = business.Business.PageType?.Name;

                    if (!string.IsNullOrEmpty(templateName))
                        controller = templateName;
                }
            }

            if (controller == null) 
                controller = _errorController;

            return (controller, pageId);
        }
    }


    public class RegionValue
    {
        public string Region { get; set; }
        public string PagesApiUrl { get; set; }
        public string BusinessApiUrl { get; set; }
    }
}
