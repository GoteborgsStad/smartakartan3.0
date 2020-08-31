using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Ganss.XSS;
using HtmlAgilityPack;

namespace SmartMap.Web.Util
{
    public static class StringHelper
    {
        public static int ReturnId(string weirdlyFormattedWpWebhookValue)
        {
            var r = new Regex("(\\\".*?\\\")");
            var idString = r.Match(weirdlyFormattedWpWebhookValue).Value.Replace("\"", "");
            return int.TryParse(idString, out int id) ? id : -1;
        }

        // url: https://stackoverflow.com/a/18154046/99769
        public static string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var result = Regex.Replace(input, "<.*?>", string.Empty);
            return WebUtility.HtmlDecode(result);
        }

        public static string FormatPhone(int? phone)
        {
            if (!phone.HasValue) return "";

            var phoneString = phone.Value.ToString();
            var firstChar = phoneString[0].ToString();
            var phoneLength = phoneString.Length;
            return firstChar switch
            {
                "7" => $"{phone:0###-######}",
                "8" => $"{phone:0#-########}",
                "4" => $"{phone:0##-######}",
                "3" => (phoneLength == 9) ? $"{phone:0##-#######}" : $"{phone:0##-######}",
                _ => $"{phone:0##-#######}"
            };
        }

        public static string SanitizeHtml(string html, string cmsDomain)
        {
            if (string.IsNullOrEmpty(html))
                return "";

            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("srcset");
            sanitizer.AllowedAttributes.Add("sizes");
            sanitizer.AllowedTags.Add("iframe");
            html = RemoveUnwantedHtml(html);
            html = FixWpImages(html, cmsDomain);

            return sanitizer.Sanitize(html);
        }

        private static string FixWpImages(string html, string cmsDomain)
        {
            if (!html.Contains("\"/wp-content/uploads"))
                return html;

            var sizes = "(max-width: 576px) 85vw, 70vw";
            html = html
                .Replace("src=\"/wp-content/uploads", $"sizes=\"{sizes}\" src=\"{cmsDomain}/wp-content/uploads")
                .Replace("srcset=\"/wp-content/uploads", $"srcset=\"{cmsDomain}/wp-content/uploads")
                .Replace(", /wp-content/uploads", $", {cmsDomain}/wp-content/uploads");
            return html;
        }

        private static string RemoveUnwantedHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var elementsWithStyleAttribute = doc.DocumentNode.SelectNodes("//@style");
            if (elementsWithStyleAttribute == null)
                return html;

            foreach (var element in elementsWithStyleAttribute)
                element.Attributes["style"].Remove();

            doc.DocumentNode.Descendants()
                .Where(n => n.NodeType == HtmlNodeType.Comment)
                .ToList()
                .ForEach(n => n.Remove());

            return doc.DocumentNode.WriteTo();
        }
    }
}
