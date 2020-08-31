using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public abstract class BaseElasticsearchRepository<T> where T : BaseElasticModel
    {
        private readonly string _indexName;
        protected readonly ILogger _logger;
        protected readonly ElasticClient _client;

        public BaseElasticsearchRepository(ILogger logger, IConfiguration configuration, string indexName)
        {
            _logger = logger;
            _indexName = indexName;
            var url = configuration["web-app:elastic-url"];
            var username = configuration["web-app:elastic-username"];
            var password = configuration["web-app:elastic-password"];

            var settings = new ConnectionSettings(new Uri(url))
#if !DEBUG
                .BasicAuthentication(username, password)
#endif
                .DefaultIndex(_indexName);

            _client = new ElasticClient(settings);
        }


        public async Task<bool> Insert(IList<T> models)
        {
            var response = await IndexMany(models);
            if (!response.Errors) return true;

            _logger.LogError("Error when inserting to Elasticsearch. {Debug}", response.DebugInformation);
            return false;
        }

        public async Task<bool> Update(T model)
        {
            var response = await _client.UpdateAsync<T>(model.Id, u => u.Index(_indexName)
                .Doc(model));

            if (response.IsValid) return true;

            _logger.LogError("Error/invalid when updating index to Elasticsearch. {Debug}", response.DebugInformation);
            return false;
        }

        private async Task<BulkResponse> IndexMany(IEnumerable<T> models)
        {
            var descriptor = new BulkDescriptor();
            descriptor.IndexMany(models, (bd, q) => bd
                .Index(_indexName)
                .Id(q.Id.ToString())
            );

            return await _client.BulkAsync(descriptor);
        }

        public async Task<bool> DeleteIndex()
        {
            var response = await _client.DeleteByQueryAsync<T>(del => del
                .Query(q => q.QueryString(qs => qs.Query("*")))
            );

            if (response.IsValid) return true;

            _logger.LogError("Error/invalid when deleting index to Elasticsearch. {Debug}", response.DebugInformation);
            return false;
        }

        public async Task<bool> Delete(int id)
        {
            var response = await _client.DeleteByQueryAsync<T>(del => del
                .Query(q => q.Match(m => m
                    .Field(f => f.Id)
                    .Query(id.ToString())
                ))
            );

            if (response.IsValid) return true;

            _logger.LogError("Error/invalid when deleting index to Elasticsearch. {Debug}", response.DebugInformation);
            return false;
        }

        public async Task<T> Get(int id)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Id)
                        .Query(id.ToString())
                    )
                )
            );

            if (response.IsValid) return response.Documents?.FirstOrDefault();

            _logger.LogError("Response invalid when searching items with matching id:{Id} in Elasticsearch. {Debug}", id, response.DebugInformation);
            return null;
        }

        public async Task<IList<T>> GetAll(int from = 0, int size = CmsVariable.ElasticSize)
        {
            var response = await _client.SearchAsync<T>(s => s
                .RequestConfiguration(r => r
                    .DisableDirectStreaming()
                )
                .From(from)
                .Size(size)
                .MatchAll()
            );

            if (!response.IsValid)
            {
                var errorMessage = "Response invalid when getting all in Elasticsearch.";
                _logger.LogError(errorMessage + " {Debug}", response.DebugInformation);
                throw new Exception(errorMessage);
            }

            var list = response.Hits.Select(x => x.Source).ToList();
            return list;
        }
    }
}
