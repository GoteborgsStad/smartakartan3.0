using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartMap.Web.Util;
using SmartMap.Web.Controllers;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.ViewModels;

namespace SmartMap.Web.Controllers
{
    public class BusinessPageController : BaseController<BusinessPageController>
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly ICmsApiProxy _cmsApiProxy;
        private readonly string _cmsDomain;

        public BusinessPageController(ILogger<BusinessPageController> logger, 
            IBusinessRepository businessRepository, 
            ICmsApiProxy cmsApiProxy, 
            IConfiguration configuration) : base(logger)
        {
            _businessRepository = businessRepository;
            _cmsApiProxy = cmsApiProxy;
            _cmsDomain = configuration["web-cms:base-url"];
        }

        public async Task<IActionResult> Index()
        {
            var page = await _businessRepository.Get(_pageId);
            if (page == null)
            {
                _logger.LogWarning($"Page not found. Business with id:{_pageId} not found in elastic.");
                return View(new BusinessPageViewModel
                {
                    Header = "Not Found"
                });
            }

            var requestPath = Request?.Path.ToString();
            if (requestPath != null && !requestPath.StartsWith(page.DetailPageLink))
            {
                // 302 Redirect
                return Redirect(page.DetailPageLink);
            }

            var addressAndCoordinates = page.AddressAndCoordinates?
                .Select(x => new BusinessPageViewModel.AddressAndCoordinateModel
                {
                    Address = x.Address,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude
                }).ToList();

            var addressAndCoordinatesJson = JsonConvert.SerializeObject(addressAndCoordinates)?.ToLower();

            var language = RouteData?.Values["language"]?.ToString();
            var translations = await _cmsApiProxy.GetTranslationsByPrefix(language, "businesspage.");

            var htmlDescription = StringHelper.SanitizeHtml(page.Description, _cmsDomain);
            var model = new BusinessPageViewModel
            {
                Translations = translations,
                Header = page.Header,
                ShortDescription = page.ShortDescription,
                Description = htmlDescription,
                City = page.City.Name,
                Area = page.Area,
                Email = page.Email,
                OnlineOnly = page.OnlineOnly ?? false,
                FacebookUrl = page.FacebookUrl,
                WebsiteUrl = page.WebsiteUrl,
                InstagramUsername = page.InstagramUsername,
                PhoneFormatted = StringHelper.FormatPhone(page.Phone),
                Phone = page.Phone,
                Tags = page.Tags,
                AddressAndCoordinatesJson = addressAndCoordinatesJson,
                AddressAndCoordinates = addressAndCoordinates,
                HasImage = page.Image?.ImageId != null,
                Image = new BusinessPageViewModel.ImageModel
                {
                    ImageUrl = page.Image?.Large?.Url,
                    AltText = page.Image?.AltText,
                    ImageId = page.Image?.ImageId
                },
                OpeningHours = new BusinessPageViewModel.OpeningHoursModel
                {
                    AlwaysOpen = page.OpeningHours?.AlwaysOpen ?? false,
                    HideOpeningHours = page.OpeningHours?.HideOpeningHours ?? false,
                    TextForOpeningHours = page.OpeningHours?.TextForOpeningHours,
                },
                Ogp = new OgpViewModel
                {
                    Title = page.Header,
                    Description = page.ShortDescription,
                    Type = "article",
                    ImageUrl = page.Image?.Large?.Url,
                    ImageHeight = page.Image?.Large?.Height,
                    ImageWidth = page.Image?.Large?.Width,
                    Url = Request.GetDisplayUrl()
                }
            };

            var closedTranslation = translations["businesspage.closed"];

            model.OpeningHours.Days = new List<BusinessPageViewModel.DayInfoModel>();
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.monday"],
                Closed = page.OpeningHours?.ClosedOnMonday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourMonday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourMonday),
                HoursText = (page.OpeningHours?.ClosedOnMonday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourMonday)} - {FormatTime(page.OpeningHours?.ClosingHourMonday)}"
            });
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.tuesday"],
                Closed = page.OpeningHours?.ClosedOnTuesday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourTuesday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourTuesday),
                HoursText = (page.OpeningHours?.ClosedOnTuesday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourTuesday)} - {FormatTime(page.OpeningHours?.ClosingHourTuesday)}"
            });
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.wednesday"],
                Closed = page.OpeningHours?.ClosedOnWednesday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourWednesday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourWednesday),
                HoursText = (page.OpeningHours?.ClosedOnWednesday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourWednesday)} - {FormatTime(page.OpeningHours?.ClosingHourWednesday)}"
            });
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.thursday"],
                Closed = page.OpeningHours?.ClosedOnThursday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourThursday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourThursday),
                HoursText = (page.OpeningHours?.ClosedOnThursday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourThursday)} - {FormatTime(page.OpeningHours?.ClosingHourThursday)}"
            });
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.friday"],
                Closed = page.OpeningHours?.ClosedOnFriday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourFriday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourFriday),
                HoursText = (page.OpeningHours?.ClosedOnFriday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourFriday)} - {FormatTime(page.OpeningHours?.ClosingHourFriday)}"
            });
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.saturday"],
                Closed = page.OpeningHours?.ClosedOnSaturday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourSaturday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourSaturday),
                HoursText = (page.OpeningHours?.ClosedOnSaturday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourSaturday)} - {FormatTime(page.OpeningHours?.ClosingHourSaturday)}"
            });
            model.OpeningHours.Days.Add(new BusinessPageViewModel.DayInfoModel
            {
                DayText = translations["businesspage.sunday"],
                Closed = page.OpeningHours?.ClosedOnSunday ?? false,
                OpeningHour = FormatTime(page.OpeningHours?.OpeningHourSunday),
                ClosingHour = FormatTime(page.OpeningHours?.ClosingHourSunday),
                HoursText = (page.OpeningHours?.ClosedOnSunday ?? false)
                    ? closedTranslation
                    : $"{FormatTime(page.OpeningHours?.OpeningHourSunday)} - {FormatTime(page.OpeningHours?.ClosingHourSunday)}"
            });

            return View(model);
        }

        private string FormatTime(TimeSpan? time)
        {
            return time != null ? $"{time.Value:hh\\:mm}" : "";
        }
    }
}