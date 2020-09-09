using System.Text.RegularExpressions;
using SmartMap.Web.Util;
using Xunit;

namespace SmartMap.Test
{
    public class RegexUnitTest
    {
        [Theory]
        [InlineData("a:1:{i:0;s:2:\"20\";}", 20)]
        [InlineData("a:1:{i:0;s:3:\"256\";}", 256)]
        [InlineData("a:1:{i:0;s:3:\"127\";}", 127)]
        public void Test_regex_for_extracting_id_from_cms_string(string value, int expected)
        {
            Regex r = new Regex("(\\\".*?\\\")");
            var idString = r.Match(value).Value.Replace("\"", "");
            int.TryParse(idString, out int id);

            Assert.Equal(expected, id);
        }

        [Theory]
        [InlineData("a:1:{i:0;s:2:\"20\";}", 20)]
        [InlineData("a:1:{i:0;s:3:\"256\";}", 256)]
        [InlineData("a:1:{i:0;s:3:\"127\";}", 127)]
        public void Test_method_for_extracting_id_from_cms_string(string value, int expected)
        {
            var id = StringHelper.ReturnId(value);

            Assert.Equal(expected, id);
        }

        [Theory]
        [InlineData(851111100, "08-51111100")]
        [InlineData(738000000, "0738-000000")]
        [InlineData(40000000, "040-000000")]
        [InlineData(104000000, "010-4000000")]
        [InlineData(313969696, "031-3969696")]
        [InlineData(31332211, "031-332211")]
        [InlineData(60550011, "060-550011")]
        [InlineData(54112233, "054-112233")]
        [InlineData(null, "")]
        public void Test_method_for_formatting_phonenumbers(long? value, string expected)
        {
            var phone = StringHelper.FormatPhone(value);

            Assert.Equal(expected, phone);
        }
    }
}
