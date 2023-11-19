using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Config;


namespace ChatTranslator
{
    public class ModConfig
    {
        private static readonly string _path = Path.Combine(GamePaths.ModConfig, "chattranslator.json") ;

        public bool Enabled { get; set; }

        public string OwnLanguage { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }

        private static readonly ModConfig Default = new ModConfig
        {
            Enabled = true,
            OwnLanguage = Lang.CurrentLocale,
            SourceLanguage = "auto",
            TargetLanguage = Lang.DefaultLocale,
        };


        public static ModConfig LoadFromDisk()
        {
            if (!File.Exists(_path))
            {
                Default.SaveToDisk();
                return Default;
            }

            var config = JsonConvert.DeserializeObject<ModConfig>(File.ReadAllText(_path));

            config.OwnLanguage ??= Default.OwnLanguage;
            config.SourceLanguage ??= Default.SourceLanguage;
            config.TargetLanguage ??= Default.TargetLanguage;
            
            return config;
        }

        public void SaveToDisk()
        {
            var toSave = new ModConfig
            {
                Enabled = Enabled,
                OwnLanguage = OwnLanguage == Default.OwnLanguage ? null : OwnLanguage,
                SourceLanguage = SourceLanguage,
                TargetLanguage = TargetLanguage,
            };

            GamePaths.EnsurePathExists(GamePaths.ModConfig);
            File.WriteAllText(_path, JsonConvert.SerializeObject(toSave, Formatting.Indented));
        }
    }
}
