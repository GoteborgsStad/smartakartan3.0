using System.Collections.Generic;

namespace SmartMap.Web.ViewModels
{
    public class TopBarViewModel
    {
        public List<RegionViewModel> Regions { get; set; }
        public List<PageViewModel> Pages { get; set; }
        public string Region { get; set; }
        public string RegionUrl { get; set; }
        public string LanguageCode { get; set; }
        public string BasePartialUrl { get; set; }
        public string RootUrl { get; set; }
        public Dictionary<string, string> Translations { get; set; }

        public class RegionViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Active { get; set; }
            public string UrlPath { get; set; }
        }

        public class PageViewModel
        {
            public int Id { get; set; }
            public string PageName { get; set; }
            public string UrlPath { get; set; }
        }
    }
}
