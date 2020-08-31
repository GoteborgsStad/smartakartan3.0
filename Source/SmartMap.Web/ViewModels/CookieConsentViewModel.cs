using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SmartMap.Web.ViewModels
{
    public class CookieConsentViewModel
    {
        public Dictionary<string, string> Translations { get; set; }
        public HttpContext Context { get; set; }
        public bool ShowCookieConsent { get; set; }
    }
}
