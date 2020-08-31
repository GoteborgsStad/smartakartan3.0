using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public interface ICmsApiProxy
    {
        Task<IList<PageCmsModel>> GetPages(string languageCode = null, string regionPageApiPath = CmsVariable.DefaultPageApiPath);
        Task<PageCmsModel> GetPage(int pageId, string regionPageApiPath = CmsVariable.DefaultPageApiPath);
        Task<IList<RegionCmsModel>> GetRegions(string languageCode = null, bool allLanguages = false);
        Task<RegionCmsModel> GetRegion(int id);
        void RemovePageCache(int pageId);
        void RemovePagesCache(string regionPageApiPath = CmsVariable.DefaultPageApiPath);
        Task<BusinessCmsModel> GetBusiness(int businessPageId, string businessApiUrl = CmsVariable.DefaultBusinessApiPath);
        Task<IList<BusinessCmsModel>> GetBusinesses(string pageBusinessApi);
        Task<IList<LanguageCmsModel>> GetLanguages();
        Task<Dictionary<string, string>> GetTranslationsByPrefix(string languageCode, string keyPrefix);
        Task<IList<PageTypeCmsModel>> GetPageType();
        Task<IList<MediaCmsModel>> GetMediaList();
        Task<IList<TagGroupCmsModel>> GetTagGroups();
        Task<TagCmsModel> GetTag(int tagId);
        Task<IList<TagCmsModel>> GetTags(string languageCode);
        Task<IList<TranslationCmsModel>> GetTranslations();
        void RemoveTranslationsCache();
        Task<(string pageApiUrl, string pageBusinessApi, string title, IList<RegionCmsModel> regions)> GetRegionList(string region, string languageCode = null);
        Task<string> ExtractLanguageFromUrl(string url);
    }
}