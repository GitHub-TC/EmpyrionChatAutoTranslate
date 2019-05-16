using EmpyrionNetAPIDefinitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

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

        static int DelayCounter;

        public static string Translate(string aSourceLanguage, string aTargetLanguage, string aText, Dictionary<string, string> aCache, int aDelayRequest)
        {
            lock (aCache)
            {
                string Result;
                if (aCache.TryGetValue(aSourceLanguage + "/" + aTargetLanguage, out Result)) return Result;

                if (aCache.Count == 0) DelayCounter = 0;

                Thread.Sleep(DelayCounter++ * aDelayRequest * 1000);

                Result = Translate(aSourceLanguage, aTargetLanguage, aText);
                aCache.Add(aSourceLanguage + "/" + aTargetLanguage, string.Compare(aText, Result, StringComparison.InvariantCultureIgnoreCase) == 0 ? null : Result);

                return Result;
            }
        }

        public static string Translate(string aSourceLanguage, string aTargetLanguage, string aText)
        {
            if (string.IsNullOrEmpty(TranslateServiceUrl)) return aText;

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