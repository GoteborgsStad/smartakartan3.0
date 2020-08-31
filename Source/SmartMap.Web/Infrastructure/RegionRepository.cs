using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public class RegionRepository : BaseElasticsearchRepository<RegionElasticModel>, IRegionRepository
    {
        private const string IndexName = "sk-region";

        public RegionRepository(ILogger<RegionRepository> logger, IConfiguration configuration) : base(logger, configuration, IndexName)
        { }


        public async Task<IList<RegionElasticModel>> GetByLanguageCode(string languageCode = CmsVariable.DefaultLanguageCode, bool allLanguages = false)
        {
            var regions = await GetAll();

            if (regions == null || !regions.Any())
                return null;

            if (allLanguages)
                return regions.Where(x => !x.Hidden).ToList();

            if (string.IsNullOrEmpty(languageCode))
                languageCode = CmsVariable.DefaultLanguageCode;

            return regions.Where(x => x.LanguageCode == languageCode && !x.Hidden).ToList();
        }

        public async Task<RegionElasticModel> GetByName(string name, string languageCode)
        {
            var regions = await GetAll();

            if (regions == null || !regions.Any())
                return null;

            return regions.SingleOrDefault(x => x.LanguageCode == languageCode && x.Name == name && !x.Hidden);
        }
    }
}
