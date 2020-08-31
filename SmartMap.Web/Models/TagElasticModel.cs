using SmartMap.Web.Models;

namespace SmartMap.Web.Models
{
    public class TagElasticModel : BaseElasticModel
    {
        public string Name { get; set; }
        public string LanguageCode { get; set; }
        public string Slug { get; set; }
        public int? TagGroupId { get; set; }
    }
}
