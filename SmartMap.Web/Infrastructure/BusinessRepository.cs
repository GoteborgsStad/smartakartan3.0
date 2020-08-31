using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Infrastructure
{
    public class BusinessRepository : BaseElasticsearchRepository<BusinessElasticModel>, IBusinessRepository
    {
        private const string IndexName = "sk-businesses-api";

        public BusinessRepository(ILogger<BusinessRepository> logger, IConfiguration configuration) : base(logger, configuration, IndexName)
        { }

        public async Task<IList<BusinessCoordinateElasticModel>> GetAllBusinessCoordinates(
            int from = 0, 
            int size = CmsVariable.ElasticSize,
            string query = null,
            string tags = null,
            string transactionTags = null,
            int[] regionIds = null,
            bool? digital = null,
            bool openNow = false,
            string languageCode = CmsVariable.DefaultLanguageCode)
        {
            var filters = new List<Func<QueryContainerDescriptor<BusinessElasticModel>, QueryContainer>>();

            if (openNow)
                filters.Add(TimeRange());

            // url: https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/returned-fields.html
            var response = await _client.SearchAsync<BusinessElasticModel>(s => s
                .RequestConfiguration(r => r
                    .DisableDirectStreaming()
                )
                .From(from)
                .Size(size)
                .Query(q => (
                                q.MultiMatch(m => m
                                    .Fields(f => f
                                        .Field(c => c.Header, 1)
                                        .Field(c => c.Tags, 2)
                                        .Field(c => c.Area, 2)
                                        .Field(c => c.ShortDescription, 2)
                                        .Field(c => c.Description, 3)
                                        .Field(c => c.AddressAndCoordinates.First().Address, 3)
                                    )
                                    .Query(query)
                                    .Operator(Operator.Or))
                                &&
                                q.Match(m => m
                                    .Field(f => f.Tags)
                                    .Operator(Operator.And)
                                    .Query(tags))
                                &&
                                q.Match(m => m
                                    .Field(f => f.Tags)
                                    .Operator(Operator.Or)
                                    .Query(transactionTags))
                            )
                            &&
                            (
                                q.Bool(b => b
                                    .Must(
                                        mu => mu.Match(m => m.Field(f => f.LanguageCode).Query(languageCode?.ToLower())),
                                        mu => mu.Terms(t => t.Field(f => f.City.Id).Terms(regionIds)),
                                        mu => mu.Match(m => m.Field(f => f.OnlineOnly).Query(digital?.ToString().ToLower())),
                                        mu => mu.Exists(x => x.Field(f => f.AddressAndCoordinates))
                                    ))
                            )
                            &&
                            (
                                q.Bool(b => b.Filter(filters))
                            )
                      )
            );

            if (!response.IsValid)
            {
                var errorMessage = "Response invalid when searching businesses in Elasticsearch.";
                _logger.LogError(errorMessage + " {Debug}", response.DebugInformation);
                throw new Exception(errorMessage);
            }

            var logMessage = $"Query:{query}, regionIds:{regionIds}, LanguageCode:{languageCode}, tags:{tags}, digital:{digital}";
            _logger.LogInformation("Search businesses coordinates with filter {LogMessage} found {Count} items.", logMessage, response.Documents?.Count);

            var list = response.Hits.Select(x => new BusinessCoordinateElasticModel
            {
                Id = x.Source.Id,
                Header = x.Source.Header,
                ShortDescription = x.Source.ShortDescription,
                DetailPageLink = x.Source.DetailPageLink,
                AddressAndCoordinates = x.Source.AddressAndCoordinates
            });

            return list.ToList();
        }

        public async Task<BusinessElasticReturnModel> GetBusinesses(
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
            string languageCode = CmsVariable.DefaultLanguageCode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ISearchResponse<BusinessElasticModel> response;

            Func<AggregationContainerDescriptor<BusinessElasticModel>, IAggregationContainer> aggregation = a => 
                a.Terms("by_tags", f => f.Field(c => c.Tags.Suffix("keyword")).Size(1000));

            if (sorting == BusinessSorting.Random)
            {
                response = await _client.SearchAsync<BusinessElasticModel>(s => s
                    .RequestConfiguration(r => r.DisableDirectStreaming())
                    .From(from)
                    .Size(size)
                    .Aggregations(aggregation)
                    .Query(oq =>
                        oq.FunctionScore(fs => fs
                            .Query(MainQuery(query, tags, transactionTags, regionIds, digital, openNow, languageCode))
                            .Functions(f => f.RandomScore(rs => rs.Seed(randomSeed))))
                    )
                );
            }
            else
            {
                response = await _client.SearchAsync<BusinessElasticModel>(s => s
                    .RequestConfiguration(r => r.DisableDirectStreaming())
                    .From(from)
                    .Size(size)
                    .Aggregations(aggregation)
                    .Query(MainQuery(query, tags, transactionTags, regionIds, digital, openNow, languageCode))
                    .Sort(ss => ss.Field(f => SortFromUserInput(f, sorting)))
                );
            }

            stopwatch.Stop();

            if (!response.IsValid)
            {
                var errorMessage = $"Response invalid when searching businesses in Elasticsearch.";
                _logger.LogError(errorMessage + " {Debug}", response.DebugInformation);
                throw new Exception(errorMessage);
            }

            var logMessage = $"Query:{query}, regionIds:{regionIds}, LanguageCode:{languageCode}, tags:{tags}, digital:{digital}";
            _logger.LogInformation("Search businesses with filter {LogMessage} found {Count} items. Took {ElapsedTime} milliseconds.", logMessage, response.Documents?.Count, stopwatch?.Elapsed.TotalMilliseconds);

            var list = response.Hits.Select(x => new BusinessElasticReturnItemModel
            {
                Business = x.Source,
                Score = x.Score
            });

            var byTags = response.Aggregations.Terms("by_tags").Buckets.Select(x => new BusinessElasticTagBucketModel
            {
                Key = x.Key,
                DocCount = x.DocCount ?? 0
            }).ToList();

            return new BusinessElasticReturnModel
            {
                Items = list.ToList(),
                Total = response.Total,
                TagCounts = byTags
            };
        }

        private Func<QueryContainerDescriptor<BusinessElasticModel>, QueryContainer> MainQuery(
            string query = null,
            string tags = null,
            string transactionTags = null,
            int[] regionIds = null,
            bool? digital = null,
            bool openNow = false,
            string languageCode = CmsVariable.DefaultLanguageCode)
        {
            Func<QueryContainerDescriptor<BusinessElasticModel>, QueryContainer> filter = null;

            if (openNow)
                filter = TimeRange();

            Func<QueryContainerDescriptor<BusinessElasticModel>, QueryContainer> queryContainer = q => 
              (
                  q.MultiMatch(m => m
                      .Fields(f => f
                          .Field(c => c.Header, 1)
                          .Field(c => c.Tags, 2)
                          .Field(c => c.Area, 2)
                          .Field(c => c.ShortDescription, 2)
                          .Field(c => c.Description, 3)
                          .Field(c => c.AddressAndCoordinates.First().Address, 3)
                      )
                      .Query(query)
                      .Operator(Operator.Or))
                  &&
                  q.Match(m => m
                      .Field(f => f.Tags)
                      .Operator(Operator.And)
                      .Query(tags))
                  &&
                  q.Match(m => m
                      .Field(f => f.Tags)
                      .Operator(Operator.Or)
                      .Query(transactionTags))
              )
              &&
              (
                  q.Bool(b => b
                      .Must(
                          mu => mu.Match(m => m.Field(f => f.LanguageCode).Query(languageCode?.ToLower())),
                          mu => mu.Terms(t => t.Field(f => f.City.Id).Terms(regionIds)),
                          mu => mu.Match(m => m.Field(f => f.OnlineOnly).Query(digital?.ToString().ToLower()))
                      )
                  )
              )
              &&
              (
                  q.Bool(b => b.Filter(filter))
              );

            return queryContainer;
        }

        private Func<QueryContainerDescriptor<BusinessElasticModel>, QueryContainer> TimeRange()
        {
            Func<QueryContainerDescriptor<BusinessElasticModel>, QueryContainer> timeFilter = null;
            var now = DateTime.Now;
            var time = now.TimeOfDay;

            Field openingHourDay = null;
            Field closingHourDay = null;

            switch (now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourMonday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourMonday);
                    break;
                case DayOfWeek.Tuesday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourTuesday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourTuesday);
                    break;
                case DayOfWeek.Wednesday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourWednesday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourWednesday);
                    break;
                case DayOfWeek.Thursday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourThursday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourThursday);
                    break;
                case DayOfWeek.Friday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourFriday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourFriday);
                    break;
                case DayOfWeek.Saturday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourSaturday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourSaturday);
                    break;
                case DayOfWeek.Sunday:
                    openingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.OpeningHourSunday);
                    closingHourDay = Infer.Field<BusinessElasticModel>(p => p.OpeningHours.ClosingHourSunday);
                    break;
            }

            if (openingHourDay != null && closingHourDay != null)
            {
                timeFilter = fq =>
                    (fq.LongRange(d => d.Field(openingHourDay).LessThanOrEquals(time.Ticks)) &&
                     fq.LongRange(d => d.Field(closingHourDay).GreaterThanOrEquals(time.Ticks))) ||
                    (fq.Terms(t => t.Field(f => f.OpeningHours.AlwaysOpen).Terms(true)) ||
                     fq.Terms(t => t.Field(f => f.OpeningHours.HideOpeningHours).Terms(true)));
            }
            return timeFilter;
        }

        private IFieldSort SortFromUserInput(FieldSortDescriptor<BusinessElasticModel> f, BusinessSorting sorting)
        {
            switch (sorting)
            {
                case BusinessSorting.HeaderDesc:
                    f.Order(SortOrder.Descending);
                    f.Field(ff => ff.Header.Suffix("keyword"));
                    break;
                case BusinessSorting.HeaderAcs:
                    f.Order(SortOrder.Ascending);
                    f.Field(ff => ff.Header.Suffix("keyword"));
                    break;
                case BusinessSorting.LatestAdded:
                    f.Order(SortOrder.Descending);
                    f.Field(ff => ff.Created);
                    break;
                case BusinessSorting.LatestUpdated:
                    f.Order(SortOrder.Descending);
                    f.Field(ff => ff.LastUpdated);
                    break;
                default:
                    f.Field("_score");
                    f.Descending();
                    break;
            }
            return f;
        }
    }

    public enum BusinessSorting
    {
        Random,
        LatestAdded,
        LatestUpdated,
        HeaderDesc,
        HeaderAcs
    }
}
