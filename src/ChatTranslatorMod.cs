using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;


namespace ChatTranslator
{
    public class ChatTranslatorMod : ModSystem
    {
        private ICoreClientAPI _capi;
        private ITranslator _translator;
        private ModConfig _config;
        
        public override bool AllowRuntimeReload => true;
        public override bool ShouldLoad(EnumAppSide side) => side.IsClient();

        public override void StartClientSide(ICoreClientAPI capi)
        {
            _translator = new GoogleTranslator(Mod.Logger);
            
            _capi = capi;

            _config = ModConfig.LoadFromDisk();
            
            capi.Event.ChatMessage += OnChatMessage;
            
            var parsers = capi.ChatCommands.Parsers;
            capi.ChatCommands.Create("chattranslator")
                .WithRootAlias("translator")
                .WithRootAlias("ct")
                .BeginSubCommand("toggle")
                    .HandleWith(OnToggleCommand)
                .EndSubCommand()
                .BeginSubCommands("lang")
                    .WithArgs(
                        parsers.OptionalWordRange("langType", "own", "source", "target"),
                        parsers.OptionalWordRange("langCode",
                            _translator.SupportedLanguages.Append("auto").ToArray()))
                    .WithExamples(
                    ".ct lang own en", 
                    ".ct lang target zh-tw",
                    ".ct lang source ru")
                    .HandleWith(OnLanguageCommand)
                .EndSubCommand();
            
            capi.ChatCommands.Create("translate")
                .WithRootAlias("t")
                .WithArgs(parsers.All("message"))
                .HandleWith(OnTranslateCommand);
        }

        private void OnChatMessage(int groupId, string message, EnumChatType chatType, string data)
        {
            if (!_config.Enabled || chatType != EnumChatType.OthersMessage) 
                return;

            // Trying to remove nickname part from string
            var parts = Regex.Split(message, "<strong>.+:<\\/strong> ", RegexOptions.Compiled);
            if (parts.Length > 1) 
                message = parts[1];
            
            var chars = message
                // Removing diacritics caused by temporal rifts
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);

            var translatedMessage = _translator.Translate(string.Concat(chars), _config.SourceLanguage, _config.OwnLanguage);
            if (translatedMessage == null)
                return;

            // TODO: Find a way to send a message with corresponding groupid
            _capi.ShowChatMessage("[T] " + translatedMessage);
        }
        
        private TextCommandResult OnLanguageCommand(TextCommandCallingArgs args)
        {
            var langType = (string) args[0];
            var langCode = (string) args[1];
            
            // VS API bug workaround
            foreach (var argumentParser in args.Parsers) 
                argumentParser.SetValue(null);

            if (string.IsNullOrEmpty(langType))
            {
                var examples = string.Join("\n", args.Command.Examples
                    .Select(example => $"<a href=\"chattype://{example}\">{example}</a>"));

                var availableLanguages = string.Join(", ", args.Parsers[1].GetValidRange(null));
                return TextCommandResult.Success(Lang.Get("Usage: .ct lang &lt;lang type&gt; [lang code]\nExamples:\n{0}\nAvailable languages: {1}.", examples, availableLanguages));
            }
            
            var langTypeCapitalized = 
                langType.Substring(0, 1).ToUpper()
                + langType.Substring(1);
            var langProperty = _config.GetType().GetProperty(langTypeCapitalized + "Language")!;

            if (string.IsNullOrEmpty(langCode))
                return TextCommandResult.Success(
                    Lang.Get("{0} language is {1}.", langTypeCapitalized, langProperty.GetValue(_config)));

            if (langCode == "auto" && langType != "source")
                return TextCommandResult.Error(Lang.Get("{0} language cannot be 'auto'.", langTypeCapitalized));

            langProperty.SetValue(_config, langCode);
            _config.SaveToDisk();
            
            return TextCommandResult.Success(Lang.Get("{0} language set to {1}.", langTypeCapitalized, langCode));
        }
        
        private TextCommandResult OnToggleCommand(TextCommandCallingArgs args)
        {
            _config.Enabled = !_config.Enabled;
            _config.SaveToDisk();
            return TextCommandResult.Success(Lang.Get("Chat translator {0}.", _config.Enabled ? "enabled" : "disabled"));
        }
        
        private TextCommandResult OnTranslateCommand(TextCommandCallingArgs args)
        {
            var message = (string) args[0];

            if (string.IsNullOrEmpty(message)) 
                return TextCommandResult.Error(Lang.Get("No message to translate."));
            
            var translatedMessage = _translator.Translate(message, _config.OwnLanguage, _config.TargetLanguage);
            if (translatedMessage == null) 
                return TextCommandResult.Error(Lang.Get("Failed to translate the message."));

            _capi.SendChatMessage(translatedMessage);
            return TextCommandResult.Deferred;
        }
    }
}
