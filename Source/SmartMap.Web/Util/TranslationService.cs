using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartMap.Web.Infrastructure;

namespace SmartMap.Web.Util
{
    public interface ITranslationService
    {
        string GetText(string key);
        Task<string> GetTextAsync(string key);
    }

    public class TranslationService : ITranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly ICmsApiProxy _cmsApiProxy;
        private IList<TranslationCmsModel> _translations;

        public TranslationService(ILogger<TranslationService> logger, ICmsApiProxy cmsApiProxy)
        {
            _logger = logger;
            _cmsApiProxy = cmsApiProxy;
        }

        public string GetText(string key)
        {
            if (_translations == null)
                _translations = _cmsApiProxy.GetTranslations().Result;

            return _translations.SingleOrDefault(t => t.Title.Rendered == key)?.Translation_text;
        }

        public async Task<string> GetTextAsync(string key)
        {
            if (_translations == null)
                _translations = await _cmsApiProxy.GetTranslations();

            return _translations.SingleOrDefault(t => t.Title.Rendered == key)?.Translation_text;
        }
    }
}
