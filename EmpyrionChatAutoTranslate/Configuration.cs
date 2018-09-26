using EmpyrionAPIDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmpyrionChatAutoTranslate
{

    public class Configuration
    {
        public string ServerMainLanguage { get; set; } = "de";
        public string DefaultSourceLanguage { get; set; } = "auto";
        public int TranslateDisplayTime { get; set; } = 10;
        public int TranslateDelayTime { get; set; } = 1;
        public int TranslateMinTextLength { get; set; } = 10;
        public string TranslateServiceUrl { get; set; } = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={3}";
        public string TanslateRespose { get; set; } = "\"(?<translate>.+?)\",\"";
        public string[] SupressTranslatePrefixes { get; set; } = new string[] { "CB:", "/", "AM:" };
    }
}
