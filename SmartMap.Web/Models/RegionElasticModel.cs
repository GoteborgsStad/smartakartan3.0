using SmartMap.Web.Models;

namespace SmartMap.Web.Models
{
    public class RegionElasticModel : BaseElasticModel
    {
        public string Name { get; set; }
        public string LanguageCode { get; set; }
        public string Modified { get; set; }
        public string UrlPath { get; set; }
        public string BusinessesApiPath { get; set; }
        public string PagesApiPath { get; set; }
        public string WelcomeMessage { get; set; }
        public bool Hidden { get; set; }
        public int MenuOrder { get; set; }
    }
}
