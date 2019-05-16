using System.Collections.Generic;
using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EmpyrionChatAutoTranslate
{

    public class TranslationSettings
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string SelectedLanguage { get; set; }

        public override string ToString()
        {
            return $"{PlayerName}/{PlayerId}: {SelectedLanguage}";
        }

        public string ToFormatedString()
        {
            return $"[c][ff0000]{PlayerName}/{PlayerId}[-][/c]: [c][ff00ff]{SelectedLanguage}[-][/c]";
        }
    }

    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; }
        public string ServerMainLanguage { get; set; } = "de";
        public string DefaultSourceLanguage { get; set; } = "auto";
        public int TranslateDisplayTime { get; set; } = 10;
        public int TranslateDelayTime { get; set; } = 1;
        public int TranslateMinTextLength { get; set; } = 10;
        public string TranslateServiceUrl { get; set; } = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={3}";
        public string TanslateRespose { get; set; } = "\"(?<translate>.+?)\",\"";
        public string[] SupressTranslatePrefixes { get; set; } = new string[] { "CB:", "/", "AM:" };
        public List<TranslationSettings> PlayerTranslationSettings { get; set; } = new List<TranslationSettings>();
    }
}
