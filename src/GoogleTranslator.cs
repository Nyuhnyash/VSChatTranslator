using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTranslateFreeApi;
using Vintagestory.API.Common;


namespace ChatTranslator
{
    public class GoogleTranslator : ITranslator
    {
        private readonly GoogleTranslateFreeApi.GoogleTranslator translator = new GoogleTranslateFreeApi.GoogleTranslator();
        private readonly ILogger _logger;

        public GoogleTranslator(ILogger modLogger) => _logger = modLogger;

        public IEnumerable<string> SupportedLanguages => GoogleTranslateFreeApi.GoogleTranslator.LanguagesSupported
            .Select(l => l.ISO639)
            .Select(l => l.ToLower())
            .OrderBy(l => l);

        public string Translate(string message, string from, string to)
        {
            try
            {
                var result = translator.TranslateLiteAsync(
                    message,
                    GoogleTranslateFreeApi.GoogleTranslator.GetLanguageByISO(from),
                    GoogleTranslateFreeApi.GoogleTranslator.GetLanguageByISO(to)
                ).Result;

                if (result.SourceLanguage.Equals(Language.Auto)
                    && result.LanguageDetections[0].Language.Equals(result.TargetLanguage))
                    return null;

                return result.MergedTranslation;
            }
            catch (LanguageIsNotSupportedException e)
            {
                _logger.Error(e.Message);
            }
            catch (GoogleTranslateIPBannedException e)
            {
                _logger.Error(e.Message);
            }
            catch (Exception e)
            {
                _logger.Error("Failed to translate the message.");
                _logger.Debug(e.StackTrace);
            }

            return null;
        }
    }
}
