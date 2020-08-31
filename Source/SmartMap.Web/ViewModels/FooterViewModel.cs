
using System.Collections.Generic;

namespace SmartMap.Web.ViewModels
{
    public class FooterViewModel
    {
        public string Header { get; set; }
        public string RegionTitle { get; set; }
        public List<LanguageViewModel> Languages { get; set; }
        public List<PageViewModel> LeftFooterPages { get; set; }
        public List<PageViewModel> CenterFooterPages { get; set; }
        public List<PageViewModel> RightFooterPages { get; set; }
        public Dictionary<string, string> Translations { get; set; }

        public class PageViewModel
        {
            public int Id { get; set; }
            public string PageName { get; set; }
            public string UrlPath { get; set; }
        }

        public class LanguageViewModel
        {
            public LanguageViewModel(string url, string text)
            {
                Url = url;
                Text = text;
            }

            public string Url { get; set; }
            public string Text { get; set; }
        }
    }
}
