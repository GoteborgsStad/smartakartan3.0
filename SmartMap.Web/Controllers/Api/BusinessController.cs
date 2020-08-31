using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Controllers.Api
{
#if !DEBUG
    [ValidateAntiForgeryToken]
#endif
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IBusinessRepository _businessRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly ICmsApiProxy _apiProxy;
        private IList<TranslationCmsModel> _translations;

        public BusinessController(ILogger<BusinessController> logger, IBusinessRepository businessRepository, ICmsApiProxy apiProxy, ITagRepository tagRepository, IRegionRepository regionRepository)
        {
            _logger = logger;
            _businessRepository = businessRepository;
            _apiProxy = apiProxy;
            _tagRepository = tagRepository;
            _regionRepository = regionRepository;
        }

        //[ResponseCache(Duration = 60*60)]
        [HttpGet("translation")]
        public async Task<IActionResult> GetTranslations([FromQuery] string lang)
        {
            if (string.IsNullOrEmpty(lang))
                lang = CmsVariable.DefaultLanguageCode;

            if (_translations == null)
                _translations = await _apiProxy.GetTranslations();

            const int cacheHeaderMaxAgeTimeInMinutes = 60;
            Response.Headers.Add("Cache-Control", $"public,max-age={cacheHeaderMaxAgeTimeInMinutes*60},must-revalidate");

            return Ok(_translations.Where(t => t.LanguageCode == lang).Select(t => new TranslationApiModel
            {
                Key = t.Title?.Rendered?.ToLower(),
                Value = t.Translation_text
            }).ToList());
        }

        [HttpGet("transactiontags")]
        public async Task<IActionResult> GetTransactionTags([FromQuery] RequestTagApiModel model)
        {
            var tagGroups = await _apiProxy.GetTagGroups();
            var transactionTagGroup = tagGroups.SingleOrDefault(x => x.Slug == CmsVariable.TransactionSlugTagGroupName);

            if (transactionTagGroup == null)
                return NoContent();

            var languageCode = string.IsNullOrEmpty(model.Lang) ? CmsVariable.DefaultLanguageCode : model.Lang;

            var allTags = await _tagRepository.GetAll();
            allTags = allTags.Where(x => x.LanguageCode == languageCode).ToList();
            var tags = allTags.Where(x => transactionTagGroup.Taggar.Contains(x.Id)).ToList();
            return Ok(tags.Select(x => new TagApiModel
            {
                Id = x.Id, 
                Name = x.Name
            }).ToList());
        }

        [HttpGet("coordinates")]
        public async Task<IActionResult> GetBusinessCoordinates([FromQuery] RequestBusinessApiModel model)
        {
            var languageCode = string.IsNullOrEmpty(model.Lang) ? CmsVariable.DefaultLanguageCode : model.Lang;
            var regionIds = await GetRegionsIdString(languageCode, model.Region);

            var response = await _businessRepository.GetAllBusinessCoordinates(
                query: model.Query,
                tags: model.Tags,
                transactionTags: model.TransactionTags,
                regionIds: regionIds,
                digital: model.Digital ? true : (bool?)null,
                openNow: model.OpenNow,
                languageCode: languageCode);

            var list = response.Select(x => new BusinessCoordinateApiModel
            {
                Id = x.Id,
                Header = x.Header,
                Description = x.ShortDescription,
                DetailPageLink = x.DetailPageLink,
                AddressAndCoordinates = x.AddressAndCoordinates?.Select(c => new AddressAndCoordinateApiModel
                {
                    Longitude = c.Longitude,
                    Latitude = c.Latitude,
                    Address = c.Address
                })?.ToList()
            });

            return Ok(list.ToList());
        }



        [HttpGet]
        public async Task<IActionResult> GetBusinesses([FromQuery] RequestBusinessApiModel model)
        {
            var from = model.Page * CmsVariable.ItemsPerPage;
            const int size = CmsVariable.ItemsPerPage;
            var languageCode = string.IsNullOrEmpty(model.Lang) ? CmsVariable.DefaultLanguageCode : model.Lang;
            var regionIds = await GetRegionsIdString(languageCode, model.Region);

            var response = await _businessRepository.GetBusinesses(
                model.RandomSeed,
                from, 
                size, 
                query: model.Query,
                tags: model.Tags,
                transactionTags: model.TransactionTags,
                regionIds: regionIds, 
                digital: model.Digital ? true : (bool?)null,
                openNow: model.OpenNow,
                sorting: model.Sorting,
                languageCode: languageCode);

            var mainTags = await ReturnTags(CmsVariable.MainSlugTagGroupName, response.TagCounts?.ToList(), languageCode, "main");
            //var subTags = new List<TagApiModel>();
            if (!string.IsNullOrEmpty(model.Tags))
            {
                var subTags = await ReturnTags(CmsVariable.SubSlugTagGroupName, response.TagCounts?.ToList(), languageCode, "sub");
                mainTags.AddRange(subTags);
            }

            return Ok(new ResponseBusinessApiModel
            {
                Items = response.Items.Select(Map).ToList(),
                ItemsPerPage = CmsVariable.ItemsPerPage,
                Total = (int)response.Total,
                FilterTags = mainTags
                //MainTags = mainTags,
                //SubTags = subTags
            });
        }


        private async Task<int[]> GetRegionsIdString(string languageCode, string regionName)
        {
            var regions = await _regionRepository.GetByLanguageCode(languageCode);
            return string.IsNullOrEmpty(regionName) ?
                regions.Select(x => x.Id).ToArray() : 
                new int[] { regions.SingleOrDefault(x => x.Name == regionName).Id };
        }

        private async Task<List<TagApiModel>> ReturnTags(string tagGroupName, List<BusinessElasticTagBucketModel> elasticTags, string languageCode, string tagType)
        {
            var tagGroups = await _apiProxy.GetTagGroups();
            var tagGroup = tagGroups.SingleOrDefault(x => x.Slug == tagGroupName);

            if (tagGroup == null)
                return new List<TagApiModel>();

            var allTags = await _tagRepository.GetAll();
            allTags = allTags.Where(x => x.LanguageCode == languageCode).ToList();
            var groupTags = allTags.Where(x => tagGroup.Taggar.Contains(x.Id)).ToList();

            if (!groupTags.Any())
                return new List<TagApiModel>();

            var activeMenuTags = groupTags
                .Select(x => elasticTags.Find(m => m.Key == x.Name))
                .Where(x => x != null)
                .ToList();

            return activeMenuTags.Select(x => new TagApiModel
            {
                Id = groupTags.SingleOrDefault(t => t.Name == x.Key)?.Id ?? 0,
                Name = x.Key,
                Count = (int)x.DocCount,
                Type = tagType
            }).OrderByDescending(x => x.Name).ToList();
        }

        private BusinessApiModel Map(BusinessElasticReturnItemModel item)
        {
            try
            {
                var model = item.Business;

                return new BusinessApiModel
                {
                    Id = model.Id,
                    DetailPageLink = model.DetailPageLink,
                    Header = model.Header,
                    Description = model.ShortDescription,
                    Area = model.Area,
                    City = model.City?.Name,
                    OnlineOnly = model.OnlineOnly ?? false,
                    ImageUrl = model.Image?.Thumbnail?.Url,
                    ImageHtml = model.Image?.Html,
                    ImageAlt = model.Image?.AltText,
                    Tags = model.Tags,
                    AddressAndCoordinates = model.AddressAndCoordinates?.Select(x => new AddressAndCoordinateApiModel
                    {
                        Longitude = x.Longitude,
                        Latitude = x.Latitude,
                        Address = x.Address
                    })?.ToList(),
                };
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to map to business api model. {Exception}.", e);
                return null;
            }
        }
    }

    public class RequestTagApiModel
    {
        public string Lang { get; set; }
    }


    public class TranslationApiModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class TagApiModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public string Type { get; set; }
    }

    public class RequestBusinessApiModel
    {
        public int Page { get; set; } = 0;
        public string Query { get; set; }
        public string Tags { get; set; }
        public string TransactionTags { get; set; }
        public string Region { get; set; }
        public string Lang { get; set; }
        public bool Digital { get; set; }
        public bool OpenNow { get; set; }
        public long RandomSeed { get; set; }
        public BusinessSorting Sorting { get; set; } = BusinessSorting.Random;
    }

    public class ResponseBusinessApiModel
    {
        public IList<BusinessApiModel> Items { get; set; }
        public int Total { get; set; }
        public int ItemsPerPage { get; set; }
        public IList<TagApiModel> FilterTags { get; set; }
        //public IList<TagApiModel> MainTags { get; set; }
        //public IList<TagApiModel> SubTags { get; set; }
    }

    public class BusinessApiModel
    {
        public int Id { get; set; }
        public string DetailPageLink { get; set; }
        public string Header { get; set; }
        public string Description { get; set; }
        public string Area { get; set; }
        public string City { get; set; }
        public bool OnlineOnly { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);
        public string ImageUrl { get; set; }
        public string ImageHtml { get; set; }
        public IList<string> Tags { get; set; }
        public string ImageAlt { get; set; }
        public IList<AddressAndCoordinateApiModel> AddressAndCoordinates { get; set; }
    }

    public class AddressAndCoordinateApiModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
    }

    public class BusinessCoordinateApiModel
    {
        public int Id { get; set; }
        public string DetailPageLink { get; set; }
        public string Header { get; set; }
        public string Description { get; set; }
        public IList<AddressAndCoordinateApiModel> AddressAndCoordinates { get; set; }
    }
}