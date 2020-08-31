using System;

namespace SmartMap.Web.Util
{
    public class CacheVariable
    {
        public static readonly string CacheKeyPrefixRegions = "cacheRegions";
        public static readonly string CacheKeyPrefixPages = "cachePages";
        public static readonly string CacheKeyPrefixPage = "cachePage";
        public static readonly string CacheKeyPrefixPageType = "cachePageType";
        public static readonly string CacheKeyMediaList = "cacheMediaList";
        //public static readonly string CacheKeyTags = "cacheTags";
        public static readonly string CacheKeyTagGroups = "cacheTagGroups";
        public static readonly string CacheKeyTranslations = "cacheTranslations";

        public static readonly TimeSpan CacheRegionsSlidingExpiration = TimeSpan.FromDays(1);
        public static readonly TimeSpan CachePageTypeSlidingExpiration = TimeSpan.FromHours(24);
        public static readonly TimeSpan CachePagesSlidingExpiration = TimeSpan.FromHours(24);
        public static readonly TimeSpan CachePageSlidingExpiration = TimeSpan.FromHours(24);
        public static readonly TimeSpan CacheSiteMapSlidingExpiration = TimeSpan.FromHours(24);

        private static readonly int DefaultCacheSlidingExpirationMinutes = 30;
        public static readonly TimeSpan CacheMediaListSlidingExpiration = TimeSpan.FromMinutes(DefaultCacheSlidingExpirationMinutes);
        public static readonly TimeSpan CacheTranslationsSlidingExpiration = TimeSpan.FromMinutes(DefaultCacheSlidingExpirationMinutes);

        private static readonly int DefaultCacheSlidingExpirationShortMinutes = 10;
        public static readonly TimeSpan CacheTagsSlidingExpiration = TimeSpan.FromMinutes(DefaultCacheSlidingExpirationShortMinutes);
    }
}
