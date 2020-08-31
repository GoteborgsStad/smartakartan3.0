using System.Collections.Generic;
using SmartMap.Web.Infrastructure;

namespace SmartMap.Web.Models
{
    public class MediaCmsModel
    {
        public int id { get; set; }
        public Description description { get; set; }
        public string alt_text { get; set; }
        public MediaDetails media_details { get; set; }
        public TitleCmsModel Title { get; set; }
    }

    public class Description
    {
        public string rendered { get; set; }
    }

    public class Medium
    {
        public string file { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string mime_type { get; set; }
        public string source_url { get; set; }
    }

    public class Large
    {
        public string file { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string mime_type { get; set; }
        public string source_url { get; set; }
    }

    public class Thumbnail
    {
        public string file { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string mime_type { get; set; }
        public string source_url { get; set; }
    }

    public class MediumLarge
    {
        public string file { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string mime_type { get; set; }
        public string source_url { get; set; }
    }

    public class Full
    {
        public string file { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string mime_type { get; set; }
        public string source_url { get; set; }
    }

    public class Sizes
    {
        public Medium medium { get; set; }
        public Large large { get; set; }
        public Thumbnail thumbnail { get; set; }
        public MediumLarge medium_large { get; set; }
        public Full full { get; set; }
    }

    public class ImageMeta
    {
        public string aperture { get; set; }
        public string credit { get; set; }
        public string camera { get; set; }
        public string caption { get; set; }
        public string created_timestamp { get; set; }
        public string copyright { get; set; }
        public string focal_length { get; set; }
        public string iso { get; set; }
        public string shutter_speed { get; set; }
        public string title { get; set; }
        public string orientation { get; set; }
        public List<object> keywords { get; set; }
    }

    public class MediaDetails
    {
        public int width { get; set; }
        public int height { get; set; }
        public string file { get; set; }
        public Sizes sizes { get; set; }
        public ImageMeta image_meta { get; set; }
    }


}
