using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;

namespace SmartMap.Web.Infrastructure
{
    public class TagRepository : BaseElasticsearchRepository<TagElasticModel>, ITagRepository
    {
        private const string IndexName = "sk-tag-api";

        public TagRepository(ILogger<TagRepository> logger, IConfiguration configuration) : base(logger, configuration, IndexName)
        { }
    }
}
