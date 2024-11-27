using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;


namespace ChatTranslator
{
    public class ChatTranslatorMod : ModSystem
    {
        private ICoreClientAPI _capi;
        private ITranslator _translator;
        private ModConfig _config;
        
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
                        parsers.OptionalWordRange("langType", "own", "source", "target", "list"),
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
            
            TyronThreadPool.QueueTask(() => {
                // Trying to remove nickname part from string
                var nicknameMatch = Regex.Match(message, "<strong>.+:<\\/strong> ", RegexOptions.Compiled);
                var nickname = nicknameMatch.Success ? nicknameMatch.Value : string.Empty;

                var chars = message.Substring(nickname.Length)
                    // Removing diacritics caused by temporal rifts
                    .Normalize(NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
                var rawMessage = string.Concat(chars);
                
                var translatedMessage = _translator.Translate(rawMessage, _config.SourceLanguage, _config.OwnLanguage);
                if (translatedMessage == null || rawMessage == translatedMessage)
                    return;

                _capi.Event.EnqueueMainThreadTask(
                    () => _capi.ShowChatMessage($"[T] {nickname}{translatedMessage}", groupId),
                    nameof(ChatTranslatorMod));
            });
        }
        
        private TextCommandResult OnLanguageCommand(TextCommandCallingArgs args)
        {
            var langType = (string) args[0];
            var langCode = (string) args[1];
            
            // VS API bug workaround
            foreach (var argumentParser in args.Parsers) 
                argumentParser.SetValue(null);

            var supportedCodes = args.Parsers[1].GetValidRange(null);
            if (string.IsNullOrEmpty(langType))
            {
                var examples = string.Join("\n", args.Command.Examples
                    .Select(example => $"<a href=\"chattype://{example}\">{example}</a>"));
                
                return TextCommandResult.Success(Lang.Get(
                    "Usage: .ct lang &lt;lang type&gt; [lang code]\nExamples:\n{0}" +
                    "\n\n<a href=\"chattype://.chattranslator lang list\">List {1} supported language codes.</a>", 
                    examples, 
                    supportedCodes.Length));
            }

            if (langType == "list") {
                return TextCommandResult.Success(Lang.Get("Supported language codes: {0}.", string.Join(", ", supportedCodes)));
            }

            var langTypeDisplay = langType.UcFirst();
            var langProperty = typeof(ModConfig).GetProperty(langTypeDisplay + "Language")!;

            if (string.IsNullOrEmpty(langCode))
                return TextCommandResult.Success(
                    Lang.Get("{0} language is {1}.", 
                        langTypeDisplay, 
                        LangUtil.DisplayName((string)langProperty.GetValue(_config))));

            if (langCode == "auto" && langType != "source")
                return TextCommandResult.Error(Lang.Get("{0} language cannot be 'auto'.", langTypeDisplay));

            langProperty.SetValue(_config, langCode);
            _config.SaveToDisk();
            
            return TextCommandResult.Success(Lang.Get("{0} language set to {1}.", 
                langTypeDisplay, 
                LangUtil.DisplayName(langCode)));
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

            TyronThreadPool.QueueTask(() => {
                var translatedMessage = _translator.Translate(message, _config.OwnLanguage, _config.TargetLanguage);
                if (translatedMessage == null) {
                    Mod.Logger.Error(Lang.Get("Failed to translate the message."));
                    return;
                }

                _capi.SendChatMessage(translatedMessage);
            });
            
            return TextCommandResult.Deferred;
        }
    }
}
