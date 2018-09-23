using System;
using Eleon.Modding;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using EmpyrionAPIDefinitions;

namespace EmpyrionChatAutoTranslate
{
    public class ChatAutoTranslateDB
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

        public Configuration Configuration { get; set; } = new Configuration();
        public List<TranslationSettings> PlayerTranslationSettings { get; set; } = new List<TranslationSettings>();
        public static Action<string, LogLevel> LogDB { get; set; }

        private static void log(string aText, LogLevel aLevel)
        {
            LogDB?.Invoke(aText, aLevel);
        }

        public void SaveDB(string DBFileName)
        {
            var serializer = new XmlSerializer(typeof(ChatAutoTranslateDB));
            Directory.CreateDirectory(Path.GetDirectoryName(DBFileName));
            using (var writer = XmlWriter.Create(DBFileName, new XmlWriterSettings() { Indent = true, IndentChars = "  " }))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static ChatAutoTranslateDB ReadDB(string DBFileName)
        {
            if (!File.Exists(DBFileName))
            {
                log($"ChatAutoTranslateDB ReadDB not found '{DBFileName}'", LogLevel.Error);
                return new ChatAutoTranslateDB();
            }

            try
            {
                log($"ChatAutoTranslateDB ReadDB load '{DBFileName}'", LogLevel.Message);
                var serializer = new XmlSerializer(typeof(ChatAutoTranslateDB));
                using (var reader = XmlReader.Create(DBFileName))
                {
                    return (ChatAutoTranslateDB)serializer.Deserialize(reader);
                }
            }
            catch(Exception Error)
            {
                log("ChatAutoTranslateDB ReadDB" + Error.ToString(), LogLevel.Error);
                return new ChatAutoTranslateDB();
            }
        }

    }
}
