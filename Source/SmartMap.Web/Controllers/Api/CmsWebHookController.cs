using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartMap.Web.Util;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Models;
using SmartMap.Web.Util;

namespace SmartMap.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CmsWebHookController : ControllerBase
    {
        private readonly ILogger<CmsWebHookController> _logger;
        private readonly ICmsApiProxy _cmsApiProxy;
        private readonly IBusinessRepository _businessRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IRegionRepository _regionRepository;
        private readonly IConfiguration _configuration;
        private readonly string _wpImageBaseUrl;


        public CmsWebHookController(
            ILogger<CmsWebHookController> logger, 
            ICmsApiProxy cmsApiProxy, 
            IBusinessRepository businessRepository, 
            IConfiguration configuration, 
            ITagRepository tagRepository, 
            IRegionRepository regionRepository)
        {
            _logger = logger;
            _cmsApiProxy = cmsApiProxy;
            _businessRepository = businessRepository;
            _configuration = configuration;
            _tagRepository = tagRepository;
            _regionRepository = regionRepository;

            _wpImageBaseUrl = _configuration["web-cms:base-url"];
        }

        [ApiKeyWpAuth]
        [HttpPost("business/insert")]
        public async Task<IActionResult> BusinessInsert([FromBody] CmsPostWebHookApiModel model)
        {
            var (isValid, modelErrorMessage, postId) = IsModelValid(model);
            if (!isValid)
                return BadRequest(modelErrorMessage);

            var webHookTypeName = HeaderType();
            if (string.IsNullOrEmpty(webHookTypeName) || webHookTypeName != "post_create")
                return BadRequest("No webhook name provided.");

            var cityId = StringHelper.ReturnId(model.post_meta?.city?.FirstOrDefault());
            var region = await GetRegionFromId(cityId);
            var cmsBusiness = await _cmsApiProxy.GetBusiness(postId, region.Businesses_api_path);
            var elasticBusiness = await MapToElasticModel(cmsBusiness);

            var successful = await _businessRepository.Insert(new List<BusinessElasticModel> { elasticBusiness });
            if (!successful)
            {
                _logger.LogError("Failed to insert business with id:{PostId} in elasticsearch.", postId);
                return BadRequest("Failed to insert business.");
            }

            _logger.LogInformation("Business with id {PostId} inserted to elasticsearch successfully.", postId);

            return Created(elasticBusiness.Id.ToString(), elasticBusiness);
        }

        [ApiKeyWpAuth]
        [HttpPost("business/update")]
        public async Task<IActionResult> BusinessUpdate([FromBody] CmsPostWebHookApiModel model)
        {
            var (isValid, modelErrorMessage, postId) = IsModelValid(model);
            if (!isValid)
                return BadRequest(modelErrorMessage);

            var webHookTypeName = HeaderType();
            if (string.IsNullOrEmpty(webHookTypeName) || webHookTypeName != "post_update")
                return BadRequest("no webhook name provided.");

            // if "trashed" remove from elastic and cache
            if (model.post.post_status == "trash")
            {
                var removeSuccessful = await _businessRepository.Delete(postId);
                if (!removeSuccessful)
                {
                    _logger.LogError("Failed to delete business with id: {PostId} in elasticsearch.", postId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete business.");
                }
                return NoContent();
            }

            var cityId = StringHelper.ReturnId(model.post_meta?.city?.FirstOrDefault());
            var region = await GetRegionFromId(cityId);
            var cmsBusiness = await _cmsApiProxy.GetBusiness(postId, region.Businesses_api_path);
            var elasticBusiness = await MapToElasticModel(cmsBusiness);

            var successful = await _businessRepository.Update(elasticBusiness);
            if (!successful)
            {
                _logger.LogError("Failed to update business with id: {PostId} in elasticsearch.", postId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update business.");
            }

            _logger.LogInformation("Business with id {PostId} updated in elasticsearch successfully.", postId);

            return Ok();
        }

        [ApiKeyWpAuth]
        [HttpPost("business/remove")]
        public async Task<IActionResult> BusinessRemove([FromBody] CmsPostDeleteApiModel model)
        {
            if (model == null)
                return BadRequest("delete model empty or null.");

            var postId = model.post_id;

            if (postId <= 0)
                return BadRequest($"post id null. model.post_id: {postId}");

            var webHookTypeName = HeaderType();
            if (string.IsNullOrEmpty(webHookTypeName) || webHookTypeName != "post_delete")
                return BadRequest("no webhook name provided.");

            var successful = await _businessRepository.Delete(postId);
            if (!successful)
            {
                _logger.LogError("Failed to delete business with id: {PostId} in elasticsearch.", postId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete business.");
            }

            _logger.LogInformation("Business with id:{BusinessId} removed in elasticsearch successfully.", postId);

            return NoContent();
        }

        [ApiKeyAuth]
        [HttpGet("business/sync")]
        public async Task<IActionResult> SyncBusiness()
        {
            _logger.LogInformation("Start Re-syncing all businesses.");
            var regions = await _regionRepository.GetAll();
            var allBusinesses = new List<BusinessCmsModel>();

            _logger.LogInformation("Fetched all regions.");

            foreach (var r in regions)
            {
                allBusinesses.AddRange(await _cmsApiProxy.GetBusinesses(r.BusinessesApiPath));
            }
            
            if (!allBusinesses.Any())
                return BadRequest("No businesses to index.");

            var list = (await Task.WhenAll(allBusinesses.Select(MapToElasticModel)))
                .Where(result => result != null).ToList();

            if (!list.Any() || list.Count <= 2)
            {
                _logger.LogWarning("Did not find any businesses or something went wrong when fetching businesses from cms-api. Count {BusinessCount}", list.Count);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch items.");
            }

            var successful = await _businessRepository.DeleteIndex();
            if (!successful)
            {
                _logger.LogError("Failed to delete index when syncing.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete index.");
            }
            _logger.LogInformation("Deleted elastic index for businesses.");

            var successfulInsert = await _businessRepository.Insert(list);
            if (!successfulInsert)
            {
                _logger.LogCritical("Insert of business list {Cound} failed to elasticsearch. Elastic index is now empty!", list.Count);
                return StatusCode(StatusCodes.Status500InternalServerError, "Insert of business list failed.");
            }

            _logger.LogInformation("Re-sync all {Count} businesses.", list.Count);
            
            return Ok();
        }

        [ApiKeyWpAuth]
        [HttpPost("pages/update")]
        public async Task<IActionResult> PageUpdate([FromBody] CmsPostWebHookApiModel model)
        {
            if (model == null)
                return BadRequest("delete model empty or null.");

            var postId = model.post_id;

            if (postId <= 0)
                return BadRequest($"post id null. model.post_id: {postId}");

            var webHookTypeName = HeaderType();
            if (string.IsNullOrEmpty(webHookTypeName) || webHookTypeName != "post_update")
                return BadRequest("no webhook name provided.");

            var apiPath = model.post.post_type;

            _cmsApiProxy.RemovePageCache(postId);
            _cmsApiProxy.RemovePagesCache(apiPath);

            await _cmsApiProxy.GetPage(postId, apiPath);
            await _cmsApiProxy.GetPages(regionPageApiPath: apiPath);

            _logger.LogInformation("Cleared and renewed cache for page {PageId} for page api path {ApiPath}.", postId, apiPath);

            return Ok();
        }

        [ApiKeyWpAuth]
        [HttpPost("translations/update")]
        public async Task<IActionResult> TranslationUpdate([FromBody] CmsPostWebHookApiModel model)
        {
            _cmsApiProxy.RemoveTranslationsCache();
            var translations = await _cmsApiProxy.GetTranslations();

            _logger.LogInformation("Cleared and renewed cache for {Count} translations.", translations.Count);

            return Ok();
        }

        [ApiKeyWpAuth]
        [HttpPost("tags/update")]
        public async Task<IActionResult> TagsUpdate([FromBody] CmsPostWebHookApiModel model)
        {
            if (model == null)
                return BadRequest("delete model empty or null.");

            var postId = model.post_id;

            if (postId <= 0)
                return BadRequest($"post id null. model.post_id: {postId}");

            if (model.post.post_status == "trash")
            {
                var removeSuccessful = await _tagRepository.Delete(postId);
                if (!removeSuccessful)
                {
                    _logger.LogError("Failed to delete tag with id: {PostId} in elasticsearch.", postId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete tag.");
                }
                return NoContent();
            }

            var cmsTag = await _cmsApiProxy.GetTag(postId);
            var elasticTag = await MapToElasticModel(cmsTag);

            bool successful;
            var tag = await _tagRepository.Get(postId);
            if (tag == null)
            {
                successful = await _tagRepository.Insert(new List<TagElasticModel> { elasticTag });
                if (!successful)
                {
                    _logger.LogError("Failed to insert tag with id:{PostId} in elasticsearch.", postId);
                    return BadRequest("Failed to insert tag.");
                }

                _logger.LogInformation("Inserted tag with id {PostId} to elasticsearch successfully.", postId);

                return Created(elasticTag.Id.ToString(), elasticTag);
            }

            successful = await _tagRepository.Update(elasticTag);
            if (!successful)
            {
                _logger.LogError("Failed to update tag with id:{PostId} in elasticsearch.", postId);
                return BadRequest("Failed to update tag.");
            }

            _logger.LogInformation("Updated tag with id {PostId} to elasticsearch successfully.", postId);

            return Ok();
        }

        [ApiKeyAuth]
        [HttpGet("tags/sync")]
        public async Task<IActionResult> SyncTags()
        {
            var languages = await _cmsApiProxy.GetLanguages();

            if (languages == null || !languages.Any())
                return BadRequest("No languages found");

            var successful = await _tagRepository.DeleteIndex();
            if (!successful)
            {
                _logger.LogError("Failed to delete tags index from elasticsearch.");
                return BadRequest("Tags index failed to be removed.");
            }

            foreach (var l in languages)
            {
                var tags = await _cmsApiProxy.GetTags(l.Code);

                if (tags == null || !tags.Any())
                    continue;

                var elasticTags = (await Task.WhenAll(tags.Select(MapToElasticModel))).ToList();
                successful = await _tagRepository.Insert(elasticTags);
                if (!successful)
                {
                    _logger.LogError("Failed to insert {Count} tags to elasticsearch.", elasticTags.Count);
                }
            }

            return Ok();
        }

        [ApiKeyWpAuth]
        [HttpPost("regions/update")]
        public async Task<IActionResult> RegionUpdate([FromBody] CmsPostWebHookApiModel model)
        {
            if (model == null)
                return BadRequest("delete model empty or null.");

            var postId = model.post_id;

            if (postId <= 0)
                return BadRequest($"post id null. model.post_id: {postId}");

            if (model.post.post_status == "trash")
            {
                var removeSuccessful = await _regionRepository.Delete(postId);
                if (!removeSuccessful)
                {
                    _logger.LogError("Failed to delete region with id: {PostId} in elasticsearch.", postId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete region.");
                }
                return NoContent();
            }

            var cmsRegion = await _cmsApiProxy.GetRegion(postId);
            var elasticRegion = await MapToElasticModel(cmsRegion);

            bool successful;
            var region = await _regionRepository.Get(postId);
            if (region == null)
            {
                successful = await _regionRepository.Insert(new List<RegionElasticModel> { elasticRegion });
                if (!successful)
                {
                    _logger.LogError("Failed to insert region with id:{PostId} in elasticsearch.", postId);
                    return BadRequest("Failed to insert region.");
                }

                _logger.LogInformation("Inserted region with id {PostId} to elasticsearch successfully.", postId);

                return Created(elasticRegion.Id.ToString(), elasticRegion);
            }

            successful = await _regionRepository.Update(elasticRegion);
            if (!successful)
            {
                _logger.LogError("Failed to update region with id:{PostId} in elasticsearch.", postId);
                return BadRequest("Failed to update region.");
            }

            _logger.LogInformation("Updated region with id {PostId} to elasticsearch successfully.", postId);

            // TODO: FIX THIS!!!!!!
            // Update business cache!
            await SyncBusiness();

            // Update pages cache!
            var apiPath = model.post.post_type;
            _cmsApiProxy.RemovePagesCache(apiPath);
            await _cmsApiProxy.GetPages(regionPageApiPath: apiPath);

            return Ok();
        }

        [ApiKeyAuth]
        [HttpGet("regions/sync")]
        public async Task<IActionResult> SyncRegions()
        {
            var regions = await _cmsApiProxy.GetRegions(allLanguages: true);
            if (regions == null || !regions.Any())
                return BadRequest("No regions to sync!");

            var successful = await _regionRepository.DeleteIndex();
            if (!successful)
            {
                _logger.LogError("Failed to delete regions index from elasticsearch.");
                return BadRequest("Regions index failed to be removed.");
            }

            var elasticRegions = (await Task.WhenAll(regions.Select(MapToElasticModel))).ToList();
            successful = await _regionRepository.Insert(elasticRegions);
            if (!successful)
            {
                _logger.LogError("Failed to insert {Count} regions to elasticsearch.", elasticRegions.Count);
            }

            return Ok();
        }


        private async Task<RegionElasticModel> MapToElasticModel(RegionCmsModel m)
        {
            return new RegionElasticModel
            {
                Id = m.Id,
                Name = WebUtility.HtmlDecode(m.Title?.Rendered),
                LanguageCode = m.Language_code,
                Hidden = m.Hide == "1",
                WelcomeMessage = m.Welcome_message,
                Modified = m.Modified,
                UrlPath = m.Url_path,
                BusinessesApiPath = m.Businesses_api_path,
                PagesApiPath = m.Pages_api_path,
                MenuOrder = m.Region_menu_order ?? 0
            };
        }

        private async Task<TagElasticModel> MapToElasticModel(TagCmsModel m)
        {
            var languages = await _cmsApiProxy.GetLanguages();
            var defaultLanguage = languages.FirstOrDefault(x => x.Default)?.Code;

            var languageCode = await _cmsApiProxy.ExtractLanguageFromUrl(m.Link);
            if (string.IsNullOrEmpty(languageCode))
                languageCode = defaultLanguage;

            return new TagElasticModel
            {
                Id = m.Id,
                Name = WebUtility.HtmlDecode(m.Title?.Rendered),
                Slug = m.Slug,
                LanguageCode = languageCode,
                TagGroupId = m.Grupp
            };
        }

        private async Task<BusinessElasticModel> MapToElasticModel(BusinessCmsModel m)
        {
            try
            {
                var cityId = m.Acf?.City?.FirstOrDefault() != null ? m.Acf.City.FirstOrDefault() : -1;
                var region = await GetRegionFromId(cityId);
                var pageType = await GetBusinessPageType();
                var imageId = m.Acf?.Main_image?.Id;
                var cmsImage = await GetCmsImage(imageId);
                var imageSrc = cmsImage?.description?.rendered;

                var languages = await _cmsApiProxy.GetLanguages();
                var defaultLanguage = languages.FirstOrDefault(x => x.Default)?.Code;
                var languageCode = await _cmsApiProxy.ExtractLanguageFromUrl(m.Link);
                if (string.IsNullOrEmpty(languageCode))
                    languageCode = defaultLanguage;

                var langCodeUrl = $"/{languageCode}";
                if (langCodeUrl.Contains(defaultLanguage))
                    langCodeUrl = "";

                var url = string.IsNullOrEmpty(region.Url_path) || region.Url_path == CmsVariable.GlobalUrlPath ? "" : $"{region.Url_path}/";
                url = $"{langCodeUrl}/{url}{m.Slug}";

                var aac = m.Address_and_coordinate?
                    .Select(x => new AddressAndCoordinateModel
                    {
                        Address = x.Post_title,
                        Latitude = x.Latitude,
                        Longitude = x.Longitude
                    }).ToList();

                var allTags = new List<string>();
                if (m.Taggar != null)
                    allTags = m.Taggar.ToList();

                if (m.Transaktionsform != null)
                    allTags.AddRange(m.Transaktionsform);

                if (m.Huvudtaggar != null)
                    allTags.AddRange(m.Huvudtaggar);

                if (m.Subtaggar != null)
                    allTags.AddRange(m.Subtaggar);

                allTags = allTags.Select(WebUtility.HtmlDecode).ToList();

                return new BusinessElasticModel
                {
                    Id = m.Id,
                    Created = m.Date,
                    LastUpdated = m.Modified,
                    DetailPageLink = url,
                    Header = WebUtility.HtmlDecode(m.Title?.Rendered),
                    Description = m.Acf?.Description,
                    ShortDescription = m.Acf?.Short_description,
                    LanguageCode = languageCode,
                    Tags = allTags,
                    Image = new BusinessElasticModel.ImageModel
                    {
                        ImageId = imageId,
                        Html = imageSrc,
                        AltText = m.Acf?.Main_image?.Alt,
                        Thumbnail = new BusinessElasticModel.ImageModel.SingleImageModel
                        {
                            Url = GetImageUrl(m.Acf?.Main_image?.Sizes?.Thumbnail),
                            Height = cmsImage?.media_details?.sizes?.thumbnail?.height,
                            Width = cmsImage?.media_details?.sizes?.thumbnail?.width
                        },
                        Medium = new BusinessElasticModel.ImageModel.SingleImageModel
                        {
                            Url = GetImageUrl(m.Acf?.Main_image?.Sizes?.Medium),
                            Height = cmsImage?.media_details?.sizes?.medium?.height,
                            Width = cmsImage?.media_details?.sizes?.medium?.width
                        },
                        MediumLarge = new BusinessElasticModel.ImageModel.SingleImageModel
                        {
                            Url = GetImageUrl(m.Acf?.Main_image?.Sizes?.Medium_large),
                            Height = cmsImage?.media_details?.sizes?.medium_large?.height,
                            Width = cmsImage?.media_details?.sizes?.medium_large?.width
                        },
                        Large = new BusinessElasticModel.ImageModel.SingleImageModel
                        {
                            Url = GetImageUrl(m.Acf?.Main_image?.Sizes?.Large),
                            Height = cmsImage?.media_details?.sizes?.large?.height,
                            Width = cmsImage?.media_details?.sizes?.large?.width
                        }
                    },
                    Phone = m.Acf?.Phone,
                    Email = m.Acf?.Email,
                    Area = m.Acf?.Area,
                    FacebookUrl = m.Acf?.Facebook_url,
                    WebsiteUrl = m.Acf?.Website_url,
                    InstagramUsername = m.Acf?.Instagram_username,
                    OnlineOnly = m.Acf?.Online_only == "1" || m.Acf?.Online_only == "true",
                    OpeningHours = new BusinessElasticModel.OpeningHoursModel
                    {
                        AlwaysOpen = m.Acf?.Always_open,
                        HideOpeningHours = m.Acf?.Hide_opening_hours,
                        TextForOpeningHours = m.Acf?.Text_for_opening_hours,
                        ClosedOnMonday = m.Acf?.Closed_on_monday,
                        OpeningHourMonday = m.Acf?.Opening_hour_monday,
                        ClosingHourMonday = m.Acf?.Closing_hour_monday,
                        ClosedOnTuesday = m.Acf?.Closed_on_tuesday,
                        OpeningHourTuesday = m.Acf?.Opening_hour_tuesday,
                        ClosingHourTuesday = m.Acf?.Closing_hour_tuesday,
                        ClosedOnWednesday = m.Acf?.Closed_on_wednesday,
                        OpeningHourWednesday = m.Acf?.Opening_hour_wednesday,
                        ClosingHourWednesday = m.Acf?.Closing_hour_wednesday,
                        ClosedOnThursday = m.Acf?.Closed_on_thursday,
                        OpeningHourThursday = m.Acf?.Opening_hour_thursday,
                        ClosingHourThursday = m.Acf?.Closing_hour_thursday,
                        ClosedOnFriday = m.Acf?.Closed_on_friday,
                        OpeningHourFriday = m.Acf?.Opening_hour_friday,
                        ClosingHourFriday = m.Acf?.Closing_hour_friday,
                        ClosedOnSaturday = m.Acf?.Closed_on_saturday,
                        OpeningHourSaturday = m.Acf?.Opening_hour_saturday,
                        ClosingHourSaturday = m.Acf?.Closing_hour_saturday,
                        ClosedOnSunday = m.Acf?.Closed_on_sunday,
                        OpeningHourSunday = m.Acf?.Opening_hour_sunday,
                        ClosingHourSunday = m.Acf?.Closing_hour_sunday,
                    },
                    City = new BusinessElasticModel.IdAndNameModel
                    {
                        Id = cityId,
                        Name = region?.Title?.Rendered
                    },
                    PageType = new BusinessElasticModel.IdAndNameModel
                    {
                        Id = pageType.Id,
                        Name = pageType?.Template_name
                    },
                    AddressAndCoordinates = aac,
                    VisibleForCities = m.Visible_for_regions,
                };
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to map to elastic model. {Exception}.", e);
                return null;
            }
        }

        private string GetImageUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return imageUrl;
            return imageUrl.Contains("http") ? imageUrl : $"{_wpImageBaseUrl}{imageUrl}";
        }

        private async Task<MediaCmsModel> GetCmsImage(int? imageId)
        {
            if (!imageId.HasValue)
                return null;

            var mediaList = await _cmsApiProxy.GetMediaList();
            return mediaList.FirstOrDefault(x => x.id == imageId.Value);
        }

        private async Task<RegionCmsModel> GetRegionFromId(int cityId)
        {
            var regions = await _cmsApiProxy.GetRegions(allLanguages: true);
            return regions.FirstOrDefault(r => r.Id == cityId);
        }

        private async Task<PageTypeCmsModel> GetBusinessPageType()
        {
            var pageTypes = await _cmsApiProxy.GetPageType();
            return pageTypes.FirstOrDefault(x => x.TypeName == CmsVariable.BusinessPageTypeName);
        }

        private string HeaderType()
        {
            return Request.Headers["x-wp-webhook-name"];
        }

        private (bool valid, string errorMessage, int postId) IsModelValid(CmsPostWebHookApiModel apiModel)
        {
            if (apiModel == null)
            {
                _logger.LogWarning("Api model is null.");
                return (false, "", -1);
            }

            if (apiModel.post_id <= 0)
            {
                _logger.LogWarning("Post id {PostId} null when validating model.", apiModel.post_id);
                return (false, "post id null.", -1);
            }

            if (string.IsNullOrEmpty(apiModel.post?.post_type))
            {
                _logger.LogWarning("post type {PostType} empty or null.", apiModel.post?.post_type);
                return (false, "post type empty or null.", -1);
            }

            return (true, "", apiModel.post_id);
        }
    }


    public class CmsPostDeleteApiModel
    {
        public int post_id { get; set; }
    }

    public class CmsPostWebHookApiModel
    {
        public int post_id { get; set; }
        public PostModel post { get; set; }
        public PostMeta post_meta { get; set; }

        public class PostModel
        {
            public string post_type { get; set; }
            public string post_status { get; set; }
        }

        public class PostMeta
        {
            public IList<string> city { get; set; }
        }
    }
}
