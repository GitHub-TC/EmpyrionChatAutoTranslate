using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using EmpyrionAPIDefinitions;

namespace EmpyrionChatAutoTranslate
{
    public class TranslateAPI
    {
        public static string TranslateServiceUrl { get; set; }
        public static string TanslateRespose { get; set; }

        public static Action<string, LogLevel> LogDB { get; set; }

        private static void log(string aText, LogLevel aLevel)
        {
            LogDB?.Invoke(aText, aLevel);
        }

        public static string Translate(string aSourceLanguage, string aTargetLanguage, string aText, ref Dictionary<string, string> aCache)
        {
            string Result;
            if (aCache.TryGetValue(aSourceLanguage + "/" + aTargetLanguage, out Result)) return Result;

            Result = Translate(aSourceLanguage, aTargetLanguage, aText);
            aCache.Add(aSourceLanguage + "/" + aTargetLanguage, string.Compare(aText, Result, StringComparison.InvariantCultureIgnoreCase) == 0 ? null : Result);

            return Result;
        }

        public static string Translate(string aSourceLanguage, string aTargetLanguage, string aText)
        {
            string url = null;
            try
            {
                url = string.Format(TranslateServiceUrl, aSourceLanguage, aTargetLanguage, aText, Uri.EscapeDataString(aText));
                string TranslationResult = null;

                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers.Add("content-type", "application/json");
                    Stream data = client.OpenRead(url);
                    using (StreamReader messageReader = new StreamReader(data))
                    {
                        TranslationResult = messageReader.ReadToEnd();
                    }
                }

                var result = new Regex(TanslateRespose).Match(TranslationResult);

                log($"ChatAutoTranslate Translate: ({result.Success}) '{aText}'/'{Uri.EscapeDataString(aText)}' Url:{url} -> '{result.Groups["translate"]?.Value}'", LogLevel.Message);

                return result.Success ? result.Groups["translate"]?.Value : aText;
            }
            catch (Exception Error)
            {
                log($"ChatAutoTranslate Error:'{Error.Message}' Url:{url}", LogLevel.Error);
                return aText;
            }
        }
    }
}