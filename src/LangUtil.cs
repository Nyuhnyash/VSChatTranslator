using System;
using System.Globalization;


namespace ChatTranslator {
    public static class LangUtil {
        public static string DisplayName(string langCode) {
            try {
                var culture = CultureInfo.GetCultureInfo(langCode);
                var displayName = culture.DisplayName.Split(new[] { " (" }, StringSplitOptions.None)[0];
                if (langCode == "zh-cn") {
                    displayName += " (Simplified)";
                }   
                if (langCode == "zh-tw") {
                    displayName += " (Traditional)";
                }

                return displayName;
            }
            catch (CultureNotFoundException) {
                return langCode;
            }
        }
    }
}
