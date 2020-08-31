using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public interface ITagRepository
    {
        Task<IList<TagElasticModel>> GetAll(int from = 0, int size = CmsVariable.ElasticSize);
        Task<bool> Insert(IList<TagElasticModel> models);
        Task<bool> Update(TagElasticModel model);
        Task<bool> DeleteIndex();
        Task<bool> Delete(int id);
        Task<TagElasticModel> Get(int id);
    }
}
