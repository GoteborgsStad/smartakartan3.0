using LazyCache;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using SmartMap.Web.Util;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public class CmsApiProxy : ICmsApiProxy
    {
        private readonly ILogger<CmsApiProxy> _logger;
        private readonly RestClient _client;
        private readonly IAppCache _cache;

        private readonly string _partialUrl;
        private readonly string _maxPagesQuery = "per_page=100";
        private readonly string _bearerToken;
        private readonly string _baseUrl;

        public CmsApiProxy(IAppCache cache, ILogger<CmsApiProxy> logger, IConfiguration configuration)
        {
            _baseUrl = configuration["web-cms:base-url"];
            _partialUrl = configuration["web-cms:wordpress-api-partial-url"];
            _bearerToken = configuration["web-cms:wordpress-api-bearer-token"];

            _cache = cache;
            _logger = logger;
            _client = new RestClient(_baseUrl);
        }

        public async Task<IList<LanguageCmsModel>> GetLanguages()
        {
            return new List<LanguageCmsModel>
            {
                new LanguageCmsModel {Name = "English", Locale = "en_GB", Code = "en", Default = false },
                new LanguageCmsModel {Name = "Svenska", Locale = "sv_SE", Code = "sv", Default = true }
            };
        }

        public async Task<Dictionary<string, string>> GetTranslationsByPrefix(string languageCode, string keyPrefix)
        {
            var langCode = languageCode ?? CmsVariable.DefaultLanguageCode;
            var translations = await GetTranslations();
            return translations?
                .Where(t => t.LanguageCode == langCode && t.Title.Rendered.StartsWith(keyPrefix))
                .ToDictionary(f => f.Title.Rendered, f => f.Translation_text);
        }


        public async Task<IList<PageCmsModel>> GetPages(string languageCode = null, string regionPageApiPath = CmsVariable.DefaultPageApiPath)
        {
            var cacheKey = $"{CacheVariable.CacheKeyPrefixPages}_{regionPageApiPath}";
            var pages = await _cache.GetOrAddAsync(
                cacheKey,
                () => GetPagesFromApi(regionPageApiPath),
                CacheVariable.CachePagesSlidingExpiration);

            languageCode = LanguageCodeForCms(languageCode);
            return pages.Where(x => ExtractLanguageFromUrl(x.Link).Result == languageCode).ToList();
        }
        private async Task<IList<PageCmsModel>> GetPagesFromApi(string regionPageApiPath)
        {
            _logger.LogInformation("Get pages from Api.");
            var (items, _) = await ApiRequest<IList<PageCmsModel>>($"{_partialUrl}/{regionPageApiPath}?{_maxPagesQuery}&_fields=id,modified,status,link,slug,title,content.rendered,page_type");
            return items.Where(x => x.Status == CmsVariable.StatusPublish).ToList();
        }


        public async Task<PageCmsModel> GetPage(int pageId, string regionPageApiPath = CmsVariable.DefaultPageApiPath)
        {
            var cacheKey = $"{CacheVariable.CacheKeyPrefixPage}_{pageId}";
            return await _cache.GetOrAddAsync(
                cacheKey, 
                () => GetPageFromApi(pageId, regionPageApiPath), 
                CacheVariable.CachePageSlidingExpiration);
        }
        private async Task<PageCmsModel> GetPageFromApi(int pageId, string regionPageApiPath)
        {
            if (pageId <= 0)
                return null;

            if (string.IsNullOrEmpty(regionPageApiPath))
                regionPageApiPath = CmsVariable.DefaultPageApiPath;

            _logger.LogInformation("Get page {Id} from Api.", pageId);

            var (item, _) = await ApiRequest<PageCmsModel>($"{_partialUrl}/{regionPageApiPath}/{pageId}?_fields=id,modified,link,slug,title,content.rendered,page_type");
            return item;
        }


        public void RemovePageCache(int pageId)
        {
            var cacheKey = $"{CacheVariable.CacheKeyPrefixPage}_{pageId}";
            _cache.Remove(cacheKey);
        }

        public void RemovePagesCache(string regionPageApiPath = CmsVariable.DefaultPageApiPath)
        {
            var cacheKey = $"{CacheVariable.CacheKeyPrefixPages}_{regionPageApiPath}";
            _cache.Remove(cacheKey);
        }


        public async Task<IList<RegionCmsModel>> GetRegions(string languageCode = null, bool allLanguages = false)
        {
            var cacheKey = $"{CacheVariable.CacheKeyPrefixRegions}";
            var items = await _cache.GetOrAddAsync(
                cacheKey,
                GetRegionsFromApi,
                CacheVariable.CacheRegionsSlidingExpiration);

            //var items = await GetRegionsFromApi();

            IList<RegionCmsModel> list;
            if (allLanguages)
            {
                list = items.Where(x => x.Status == CmsVariable.StatusPublish
                                        && x.Hide != "1").ToList();
            }
            else
            {
                languageCode = LanguageCodeForCms(languageCode);
                list = items.Where(x => ExtractLanguageFromUrl(x.Link).Result == languageCode
                                        && x.Status == CmsVariable.StatusPublish
                                        && x.Hide != "1").ToList();
            }
            return list;
        }
        private async Task<IList<RegionCmsModel>> GetRegionsFromApi()
        {
            _logger.LogInformation("Get regions from Api.");
            var query = "_fields=id,modified,status,link,title,url_path,pages_api_path,businesses_api_path,language_code,welcome_message,region_menu_order,hide";
            var url = $"{_partialUrl}/region?{_maxPagesQuery}&{query}";
            var (items, _) = await ApiRequest<IList<RegionCmsModel>>(url);
            return items;
        }


        public async Task<RegionCmsModel> GetRegion(int id)
        {
            var query = "_fields=id,modified,status,link,title,url_path,pages_api_path,businesses_api_path,language_code,welcome_message,region_menu_order,hide";
            var url = $"{_partialUrl}/region/{id}?{query}";
            var (item, _) = await ApiRequest<RegionCmsModel>(url);
            return item;
        }

        public async Task<IList<BusinessCmsModel>> GetBusinesses(string pageBusinessApi)
        {
            // No cache
            return await GetBusinessesFromApi(pageBusinessApi);
        }
        public async Task<IList<BusinessCmsModel>> GetBusinessesFromApi(string pageBusinessApi)
        {
            var url = $"{_partialUrl}/{pageBusinessApi}?{_maxPagesQuery}";
            var (items, response) = await ApiRequest<IList<BusinessCmsModel>>(url);
            var list = items.ToList();

            var (total, totalPages) = GetPaginationHeaderValues(response);
            if (totalPages > 1)
            {
                var currentPage = 2;
                while (currentPage <= totalPages)
                {
                    (items, _) = await ApiRequest<IList<BusinessCmsModel>>($"{url}&page={currentPage}");
                    list.AddRange(items?.ToList());
                    currentPage++;
                }
            }

            return list;
        }


        public async Task<BusinessCmsModel> GetBusiness(int businessPageId, string businessApiUrl = CmsVariable.DefaultBusinessApiPath)
        {
            // No cache!
            return await GetBusinessFromApi(businessPageId, businessApiUrl);
        }
        private async Task<BusinessCmsModel> GetBusinessFromApi(int businessPageId, string businessApiUrl)
        {
            var (item, _) = await ApiRequest<BusinessCmsModel>($"{_partialUrl}/{businessApiUrl}/{businessPageId}");
            return item;
        }


        public async Task<IList<PageTypeCmsModel>> GetPageType()
        {
            var cacheKey = $"{CacheVariable.CacheKeyPrefixPageType}";
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetPageTypeFromApi,
                CacheVariable.CachePageTypeSlidingExpiration);
        }
        public async Task<IList<PageTypeCmsModel>> GetPageTypeFromApi()
        {
            var (items, _) = await ApiRequest<IList<PageTypeCmsModel>>($"{_partialUrl}/page_type?{_maxPagesQuery}&_fields=id,template_name,typename");
            return items;
        }


        public async Task<IList<MediaCmsModel>> GetMediaList()
        {
            var cacheKey = $"{CacheVariable.CacheKeyMediaList}";
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetMediaListFromApi,
                CacheVariable.CacheMediaListSlidingExpiration);
        }
        private async Task<IList<MediaCmsModel>> GetMediaListFromApi()
        {
            var url = $"{_partialUrl}/media?{_maxPagesQuery}";
            var (items, response) = await ApiRequest<IList<MediaCmsModel>>(url);
            var list = items.ToList();

            var (total, totalPages) = GetPaginationHeaderValues(response);
            if (totalPages > 1)
            {
                var currentPage = 2;
                while (currentPage <= totalPages)
                {
                    (items, _) = await ApiRequest<IList<MediaCmsModel>>($"{url}&page={currentPage}");
                    list.AddRange(items?.ToList());
                    currentPage++;
                }
            }

            //if (total != mediaList.Count)
            //    return BadRequestResult();

            return list;
        }

        public async Task<TagCmsModel> GetTag(int tagId)
        {
            var queryString = "_fields=id,title,slug,link,status,grupp";
            var url = $"{_partialUrl}/tagg/{tagId}?{queryString}";
            var (item, _) = await ApiRequest<TagCmsModel>(url);
            return item;
        }

        public async Task<IList<TagCmsModel>> GetTags(string languageCode)
        {
            return await GetTagsFromApi(languageCode);
        }
        private async Task<IList<TagCmsModel>> GetTagsFromApi(string languageCode)
        {
            var queryString = "_fields=id,title,slug,link,status,grupp";
            var endPoint = "tagg";
            var url = $"{_partialUrl}/{endPoint}?{queryString}&{_maxPagesQuery}";
            var (items, response) = await ApiRequest<IList<TagCmsModel>>(url);
            var tags = items.ToList();

            var (_, totalPages) = GetPaginationHeaderValues(response);
            if (totalPages > 1)
            {
                var currentPage = 2;
                while (currentPage <= totalPages)
                {
                    (items, _) = await ApiRequest<IList<TagCmsModel>>($"{url}&page={currentPage}");
                    tags.AddRange(items?.ToList());
                    currentPage++;
                }
            }

            languageCode = LanguageCodeForCms(languageCode);

            //if (total != mediaList.Count)
            //    return BadRequestResult();

            var list = tags.Where(r => ExtractLanguageFromUrl(r.Link).Result == languageCode
                                       && r.Status == CmsVariable.StatusPublish).ToList();
            return list;
        }


        public async Task<IList<TagGroupCmsModel>> GetTagGroups()
        {
            var cacheKey = $"{CacheVariable.CacheKeyTagGroups}";
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetTagGroupsFromApi,
                CacheVariable.CacheTagsSlidingExpiration);
        }
        private async Task<IList<TagGroupCmsModel>> GetTagGroupsFromApi()
        {
            var queryString = "_fields=id,title,slug,link,taggar,status";
            var endPoint = "tagg_grupp";
            var (items, _) = await ApiRequest<IList<TagGroupCmsModel>>($"{_partialUrl}/{endPoint}?{queryString}&{_maxPagesQuery}");
            return items.ToList();
        }


        public void RemoveTranslationsCache()
        {
            _cache.Remove(CacheVariable.CacheKeyTranslations);
        }

        public async Task<IList<TranslationCmsModel>> GetTranslations()
        {
            return await _cache.GetOrAddAsync(
                CacheVariable.CacheKeyTranslations,
                GetTranslationsFromApi,
                CacheVariable.CacheTranslationsSlidingExpiration);
        }
        private async Task<IList<TranslationCmsModel>> GetTranslationsFromApi()
        {
            _logger.LogInformation("Get translations from Api.");

            var queryString = "_fields=id,status,link,title,translation_text";
            var endPoint = "translations";
            var url = $"{_partialUrl}/{endPoint}?{queryString}&{_maxPagesQuery}";
            var (items, response) = await ApiRequest<IList<TranslationCmsModel>>(url);
            var list = items.ToList();

            var (total, totalPages) = GetPaginationHeaderValues(response);
            if (totalPages > 1)
            {
                var currentPage = 2;
                while (currentPage <= totalPages)
                {
                    (items, _) = await ApiRequest<IList<TranslationCmsModel>>($"{url}&page={currentPage}");
                    list.AddRange(items?.ToList());
                    currentPage++;
                }
            }

            foreach (var i in list)
            {
                var langCode = await ExtractLanguageFromUrl(i.Link);
                i.LanguageCode = string.IsNullOrEmpty(langCode) ? CmsVariable.DefaultLanguageCode : langCode;
            }

            return list.ToList();
        }


        public async Task<(string pageApiUrl, string pageBusinessApi, string title, IList<RegionCmsModel> regions)> 
            GetRegionList(string region, string languageCode = null)
        {
            var regions = await GetRegions(languageCode);
            var activeRegion = regions?.FirstOrDefault(r => r.Url_path == region);

            regions = regions?
                .OrderByDescending(x => x.Region_menu_order.HasValue)
                .ThenBy(x => x.Region_menu_order)
                .ToList();

            return (
                pageApiUrl: activeRegion?.Pages_api_path ?? CmsVariable.DefaultPageApiPath,
                pageBusinessApi: activeRegion?.Businesses_api_path ?? CmsVariable.DefaultBusinessApiPath,
                title: activeRegion?.Title?.Rendered ?? "",
                regions: regions
            );
        }

        private string LanguageCodeForCms(string languageCode)
        {
            return languageCode == CmsVariable.DefaultLanguageCode || languageCode == null ? "" : languageCode;
        }

        public async Task<string> ExtractLanguageFromUrl(string url)
        {
            /*
            Example:
            url: "http://localhost/index.php/en/region/gothenburg/" => "en"
            url: "http://localhost/index.php/region/goteborg/" => ""
            */
            if (string.IsNullOrEmpty(url))
                return "";

            var uriAddress = new Uri(url);
            if (uriAddress.Segments?.Length > 0)
            {
                var list = await GetLanguages();
                foreach (var uriAddressSegment in uriAddress.Segments)
                {
                    if (list.Any(l => uriAddressSegment.TrimEnd('/') == l.Code))
                        return uriAddressSegment.TrimEnd('/');
                }
            }
            return "";
        }

        private (int total, int totalPages) GetPaginationHeaderValues(IRestResponse response)
        {
            var total = int.TryParse(response.Headers.FirstOrDefault(x => x.Name == CmsVariable.WpHeaderValueTotal)?.Value?.ToString(),
                out var tot)
                ? tot
                : -1;

            var totalPages = int.TryParse(response.Headers.FirstOrDefault(x => x.Name == CmsVariable.WpHeaderValueTotalPages)?.Value?.ToString(),
                out var totp)
                ? totp
                : -1;

            return (total, totalPages);
        }

        private async Task<(T returnObject, IRestResponse<T> response)> ApiRequest<T>(string url)
        {
            try
            {
                var request = new RestRequest(url, DataFormat.Json);
                request.AddAuthorizationHeader(_bearerToken);
                var response = await _client.ExecuteTaskAsync<T>(request);
                T items = ValidateAndGetResponseData(response);
                return (items, response);
            }
            catch (Exception e)
            {
                var lastFourChars = "";
                if (!string.IsNullOrEmpty(_bearerToken))
                    lastFourChars = _bearerToken.Substring(_bearerToken.Length - 4);
                _logger.LogError("Error when executing api request. token:{Token} url:{BaseUrl}{Url}, exception:{Exception}", lastFourChars, _baseUrl, url, e);
                throw;
            }
        }

        private readonly List<string> _propertyArraysToReplaceFalseWith = new List<string>
        {
            "address_and_coordinate",
            "main_image",
            "visible_for_regions",
            "taggar",
            "grupp",
            "transaktionsform",
            "huvudtaggar",
            "subtaggar",
            "hide",
            "page_type"
        };

        private T ValidateAndGetResponseData<T>(IRestResponse<T> response)
        {
            var responseData = default(T);
            var throwOnError = true;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = response.Content;
                foreach (var propName in _propertyArraysToReplaceFalseWith)
                {
                    // Ugly fix! For some reason Wordpress (Pods plugin for custom types?) api returns false 
                    // when an array is empty! So set false to null for arrays so they can be properly deserialized.
                    if (response.Content.Contains($"\"{propName}\""))
                        content = content
                            .Replace($"\"{propName}\":false", $"\"{propName}\":null")
                            .Replace($"\"{propName}\":[]", $"\"{propName}\":\"0\"");
                }
                responseData = JsonConvert.DeserializeObject<T>(content);
                throwOnError = false;
            }

            if (response.ErrorException != null && throwOnError)
            {
                var exception = new Exception("Error retrieving response. Check inner details for more info.", response.ErrorException);
                _logger.LogError("Error retrieving response. Check inner details for more info. {InnerException}", response.ErrorException);
                throw exception;
            }
            
            if (throwOnError)
                responseData = response.Data;

            return responseData;
        }
    }

    public static class RestRequestExtender
    {
        public static void AddAuthorizationHeader(this RestRequest request, string token)
        {
            request.AddHeader("Authorization", $"Bearer {token}");
        }
    }

    public class TranslationCmsModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Link { get; set; }
        public TitleCmsModel Title { get; set; }
        public string Translation_text { get; set; }
        public string LanguageCode { get; set; }
    }

    public class TagGroupCmsModel
    {
        public int Id { get; set; }
        public string Link { get; set; }
        public TitleCmsModel Title { get; set; }
        public string Slug { get; set; }
        public string Status { get; set; }
        public IList<int> Taggar { get; set; }
    }
    public class TagCmsModel
    {
        public int Id { get; set; }
        public string Link { get; set; }
        public TitleCmsModel Title { get; set; }
        public string Slug { get; set; }
        public string Status { get; set; }
        public int? Grupp { get; set; }
    }

    public class LanguageCmsModel
    {
        public string Name { get; set; }
        public string Locale { get; set; }
        public string Code { get; set; }
        public bool Default { get; set; }
    }

    public class PageCmsModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Modified { get; set; }
        public string Link { get; set; }
        public string Slug { get; set; }
        public TitleCmsModel Title { get; set; }
        public ContentCmsModel Content { get; set; }
        public IList<PageTypeCmsModel> Page_type { get; set; }
    }

    public class RegionCmsModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Modified { get; set; }
        public string Link { get; set; }
        public TitleCmsModel Title { get; set; }
        public string Url_path { get; set; }
        public string Businesses_api_path { get; set; }
        public string Pages_api_path { get; set; }
        public string Language_code { get; set; }
        public string Welcome_message { get; set; }
        public int? Region_menu_order { get; set; }
        public string Hide { get; set; }
    }

    public class BusinessCmsModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Slug { get; set; }
        public string Link { get; set; }
        public DateTime Date { get; set; }
        public DateTime Modified { get; set; }
        public TitleCmsModel Title { get; set; }
        public IList<PageTypeCmsModel> Page_type { get; set; }
        public AdvancedCustomFieldCmsModel Acf { get; set; }
        public IList<AddressAndCoordinateCmsModel> Address_and_coordinate { get; set; }
        public IList<string> Taggar { get; set; }
        public IList<string> Transaktionsform { get; set; }
        public IList<string> Huvudtaggar { get; set; }
        public IList<string> Subtaggar { get; set; }

        // Only in global_business
        public List<int> Visible_for_regions { get; set; }
    }

    public class TitleCmsModel
    {
        public string Rendered { get; set; }
    }

    public class ContentCmsModel
    {
        public string Rendered { get; set; }
    }

    public class PageTypeCmsModel
    {
        public int Id { get; set; }
        public string Template_name { get; set; }
        public string TypeName { get; set; }
    }

    public class AdvancedCustomFieldCmsModel
    {
        public IList<int> City { get; set; }
        public IList<int> Page_type { get; set; }

        public string Short_description { get; set; }
        public string Description { get; set; }

        public bool? Hide_opening_hours { get; set; }
        public bool? Always_open { get; set; }
        public string Text_for_opening_hours { get; set; }

        public bool? Closed_on_monday { get; set; }
        public TimeSpan? Opening_hour_monday { get; set; }
        public TimeSpan? Closing_hour_monday { get; set; }

        public bool? Closed_on_tuesday { get; set; }
        public TimeSpan? Opening_hour_tuesday { get; set; }
        public TimeSpan? Closing_hour_tuesday { get; set; }

        public bool? Closed_on_wednesday { get; set; }
        public TimeSpan? Opening_hour_wednesday { get; set; }
        public TimeSpan? Closing_hour_wednesday { get; set; }

        public bool? Closed_on_thursday { get; set; }
        public TimeSpan? Opening_hour_thursday { get; set; }
        public TimeSpan? Closing_hour_thursday { get; set; }

        public bool? Closed_on_friday { get; set; }
        public TimeSpan? Opening_hour_friday { get; set; }
        public TimeSpan? Closing_hour_friday { get; set; }

        public bool? Closed_on_saturday { get; set; }
        public TimeSpan? Opening_hour_saturday { get; set; }
        public TimeSpan? Closing_hour_saturday { get; set; }

        public bool? Closed_on_sunday { get; set; }
        public TimeSpan? Opening_hour_sunday { get; set; }
        public TimeSpan? Closing_hour_sunday { get; set; }

        public string Area { get; set; }
        public string Instagram_username { get; set; }
        public string Facebook_url { get; set; }
        public string Website_url { get; set; }
        public string Online_only { get; set; }
        public string Email { get; set; }
        public long? Phone { get; set; }
        public ImageCmsModel Main_image { get; set; }
    }

    public class ImageCmsModel
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public string Link { get; set; }
        public string Url { get; set; }
        public string Mime_type { get; set; }
        public string Icon { get; set; }
        public string Alt { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public SizeModel Sizes { get; set; }

        public class SizeModel
        {
            public string Thumbnail { get; set; }
            public string Medium { get; set; }
            public string Medium_large { get; set; }
            public string Large { get; set; }

            //[JsonProperty("thumbnail-width")]
            //public int ThumbnailWidth { get; set; }
            //[JsonProperty("thumbnail-height")]
            //public int ThumbnailHeight { get; set; }
        }
    }

    public class AddressAndCoordinateCmsModel
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        //public string Address { get; set; }
        public string Post_title { get; set; }
    }
}
