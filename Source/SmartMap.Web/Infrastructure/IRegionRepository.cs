using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public interface IRegionRepository
    {
        Task<IList<RegionElasticModel>> GetAll(int from = 0, int size = CmsVariable.ElasticSize);
        Task<bool> Insert(IList<RegionElasticModel> models);
        Task<bool> Update(RegionElasticModel model);
        Task<bool> DeleteIndex();
        Task<bool> Delete(int id);
        Task<RegionElasticModel> Get(int id);
        Task<IList<RegionElasticModel>> GetByLanguageCode(string languageCode = null, bool allLanguages = false);
        Task<RegionElasticModel> GetByName(string name, string languageCode);
    }
}
