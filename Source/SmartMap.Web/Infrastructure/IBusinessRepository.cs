using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public interface IBusinessRepository
    {
        Task<IList<BusinessElasticModel>> GetAll(int from = 0, int size = CmsVariable.ElasticSize);
        Task<bool> Insert(IList<BusinessElasticModel> models);
        Task<bool> Update(BusinessElasticModel model);
        Task<bool> DeleteIndex();
        Task<bool> Delete(int id);
        Task<BusinessElasticModel> Get(int id);

        Task<IList<BusinessCoordinateElasticModel>> GetAllBusinessCoordinates(
            int from = 0,
            int size = CmsVariable.ElasticSize,
            string query = null,
            string tags = null,
            string transactionTags = null,
            int[] regionIds = null,
            bool? digital = null,
            bool openNow = false,
            string languageCode = CmsVariable.DefaultLanguageCode);

        Task<BusinessElasticReturnModel> GetBusinesses(
            long randomSeed,
            int from = 0,
            int size = 10,
            string query = null,
            string tags = null,
            string transactionTags = null,
            int[] regionIds = null,
            bool? digital = null,
            bool openNow = false,
            BusinessSorting sorting = BusinessSorting.Random,
            string languageCode = CmsVariable.DefaultLanguageCode);
    }
}
