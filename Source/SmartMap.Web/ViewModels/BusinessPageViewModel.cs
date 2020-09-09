using System.Collections.Generic;

namespace SmartMap.Web.ViewModels
{
    public class BusinessPageViewModel
    {
        public string Header { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Area { get; set; }
        public string City { get; set; }
        public bool OnlineOnly { get; set; }
        public bool HasImage { get; set; }
        public ImageModel Image { get; set; }
        public IList<AddressAndCoordinateModel> AddressAndCoordinates { get; set; }
        public string AddressAndCoordinatesJson { get; set; }
        public OpeningHoursModel OpeningHours { get; set; }
        public string Email { get; set; }
        public long? Phone { get; set; }
        public string PhoneFormatted { get; set; }
        public string InstagramUsername { get; set; }
        public string FacebookUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public IList<string> Tags { get; set; }
        public Dictionary<string, string> Translations { get; set; }
        public OgpViewModel Ogp { get; set; }

        public class AddressAndCoordinateModel
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Address { get; set; }
        }

        public class ImageModel
        {
            public int? ImageId { get; set; }
            public string Html { get; set; }
            public string AltText { get; set; }
            public string ImageUrl { get; set; }
        }

        public class OpeningHoursModel
        {
            public bool HideOpeningHours { get; set; }
            public bool AlwaysOpen { get; set; }
            public string TextForOpeningHours { get; set; }
            public List<DayInfoModel> Days { get; set; }
        }

        public class DayInfoModel
        {
            public bool Closed { get; set; }
            public string OpeningHour { get; set; }
            public string ClosingHour { get; set; }
            public string DayText { get; set; }
            public string HoursText { get; set; }
        }
    }
}
