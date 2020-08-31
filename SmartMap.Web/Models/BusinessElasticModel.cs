using System;
using System.Collections.Generic;
using SmartMap.Web.Models;

namespace SmartMap.Web.Models
{
    public class BusinessElasticReturnModel
    {
        public long Total { get; set; }
        public IList<BusinessElasticReturnItemModel> Items { get; set; }
        public IList<BusinessElasticTagBucketModel> TagCounts { get; set; }
    }

    public class BusinessElasticTagBucketModel
    {
        public string Key { get; set; }
        public long DocCount { get; set; }
    }

    public class BusinessElasticReturnItemModel
    {
        public BusinessElasticModel Business { get; set; }
        public double? Score { get; set; }
    }

    public class BusinessElasticModel : BaseElasticModel
    {
        //public int Id { get; set; }
        public string DetailPageLink { get; set; }
        public string Header { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public bool? OnlineOnly { get; set; }
        public string Area { get; set; }
        public string InstagramUsername { get; set; }
        public string FacebookUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public ImageModel Image { get; set; }
        public OpeningHoursModel OpeningHours { get; set; }
        public IdAndNameModel City { get; set; }
        public IdAndNameModel PageType { get; set; }
        public string LanguageCode { get; set; }
        public IList<AddressAndCoordinateModel> AddressAndCoordinates { get; set; }
        public string Email { get; set; }
        public int? Phone { get; set; }
        public IList<string> Tags { get; set; }
        public IList<int> VisibleForCities { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }

        public class ImageModel
        {
            public int? ImageId { get; set; }
            public string Html { get; set; }
            public string AltText { get; set; }

            public SingleImageModel Thumbnail { get; set; }
            public SingleImageModel Medium { get; set; }
            public SingleImageModel MediumLarge { get; set; }
            public SingleImageModel Large { get; set; }

            public class SingleImageModel
            {
                public string Url { get; set; }
                public int? Width { get; set; }
                public int? Height { get; set; }
            }
        }
        public class IdAndNameModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class OpeningHoursModel
        {
            public bool? HideOpeningHours { get; set; }
            public bool? AlwaysOpen { get; set; }
            public string TextForOpeningHours { get; set; }
            public bool? ClosedOnMonday { get; set; }
            public TimeSpan? OpeningHourMonday { get; set; }
            public TimeSpan? ClosingHourMonday { get; set; }
            public bool? ClosedOnTuesday { get; set; }
            public TimeSpan? OpeningHourTuesday { get; set; }
            public TimeSpan? ClosingHourTuesday { get; set; }
            public bool? ClosedOnWednesday { get; set; }
            public TimeSpan? OpeningHourWednesday { get; set; }
            public TimeSpan? ClosingHourWednesday { get; set; }
            public bool? ClosedOnThursday { get; set; }
            public TimeSpan? OpeningHourThursday { get; set; }
            public TimeSpan? ClosingHourThursday { get; set; }
            public bool? ClosedOnFriday { get; set; }
            public TimeSpan? OpeningHourFriday { get; set; }
            public TimeSpan? ClosingHourFriday { get; set; }
            public bool? ClosedOnSaturday { get; set; }
            public TimeSpan? OpeningHourSaturday { get; set; }
            public TimeSpan? ClosingHourSaturday { get; set; }
            public bool? ClosedOnSunday { get; set; }
            public TimeSpan? OpeningHourSunday { get; set; }
            public TimeSpan? ClosingHourSunday { get; set; }
        }
    }

    public class AddressAndCoordinateModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
    }

    public class BusinessCoordinateElasticModel
    {
        public int Id { get; set; }
        public string Header { get; set; }
        public string ShortDescription { get; set; }
        public string DetailPageLink { get; set; }
        public IList<AddressAndCoordinateModel> AddressAndCoordinates { get; set; }
    }
}
