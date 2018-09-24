using System;
using Eleon.Modding;
using EmpyrionAPITools;
using System.Collections.Generic;
using EmpyrionAPIDefinitions;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace EmpyrionChatAutoTranslate
{
    public static class Extensions{

        public static T GetAttribute<T>(this Assembly aAssembly)
        {
            return aAssembly.GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();
        }

        static Regex GetCommand = new Regex(@"(?<cmd>(\w|\/|\s)+)");

        public static string MsgString(this ChatCommand aCommand)
        {
            var CmdString = GetCommand.Match(aCommand.invocationPattern).Groups["cmd"]?.Value ?? aCommand.invocationPattern;
            return $"[c][ff00ff]{CmdString}[-][/c]{aCommand.paramNames.Aggregate(" ", (S, P) => S + $"<[c][00ff00]{P}[-][/c]> ")}: {aCommand.description}";
        }

    }

    public partial class EmpyrionChatAutoTranslate : SimpleMod
    {
        public ModGameAPI GameAPI { get; set; }
        public ChatAutoTranslateDB ChatAutoTranslatesDB { get; set; }

        public string ChatAutoTranslatesDBFilename { get; set; }

        FileSystemWatcher DBFileChangedWatcher;

        enum SubCommand
        {
            Help,
            Set,
            Box,
            Clear,
            ListAll
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;
            verbose = true;
            this.LogLevel = LogLevel.Message;
            TranslateAPI.LogDB = log;

            log($"**HandleEmpyrionChatAutoTranslate loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

            InitializeDB();
            InitializeDBFileWatcher();

            Event_ChatMessage += EmpyrionChatAutoTranslate_Event_ChatMessage;

            ChatCommands.Add(new ChatCommand(@"/trans help",                    (I, A) => ExecCommand(SubCommand.Help,     I, A), "Show the help"));
            ChatCommands.Add(new ChatCommand(@"/trans set (?<language>.*)",     (I, A) => ExecCommand(SubCommand.Set,      I, A), "Set the translation language"));
            ChatCommands.Add(new ChatCommand(@"/trans box (?<text>.+)",         (I, A) => ExecCommand(SubCommand.Box,      I, A), "Translate to a messagebox"));
            ChatCommands.Add(new ChatCommand(@"/trans clear",                   (I, A) => ExecCommand(SubCommand.Help,     I, A), "Back to the Serverlanguage"));
            ChatCommands.Add(new ChatCommand(@"/trans listall",                 (I, A) => ExecCommand(SubCommand.ListAll,  I, A), "List all translation settings", PermissionType.Moderator));
        }

        private void EmpyrionChatAutoTranslate_Event_ChatMessage(ChatInfo info)
        {
            var UpperMsg = info.msg.ToUpper();
            if (ChatAutoTranslatesDB.Configuration.SupressTranslatePrefixes.Any(M => UpperMsg.StartsWith(M))) return;

            log($"**HandleEmpyrionChatAutoTranslate Translate {info.type}: playerId:{info.playerId} recipientEntityId:{info.recipientEntityId} recipientFactionId:{info.recipientFactionId} msg:{info.msg}", LogLevel.Message);

            Request_Player_Info(info.playerId.ToId(), P => SendTranslateToSinglePlayer(P, info), E => InformPlayer(info.playerId, "Translate: {E}"));
        }

        private void SendTranslateToSinglePlayer(PlayerInfo aSender, ChatInfo aInfo)
        {
            var Cache = new Dictionary<string, string>();

            var aSenderTranslateInfo = ChatAutoTranslatesDB.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == aSender.entityId);
            log($"**HandleEmpyrionChatAutoTranslate Translate found {aSenderTranslateInfo?.PlayerName}:{aSenderTranslateInfo?.PlayerId} playerId:{aInfo.playerId}-> {aSenderTranslateInfo?.SelectedLanguage}", LogLevel.Message);

            Request_Player_List(L => {
                L.list.ForEach(PI => Request_Player_Info(PI.ToId(), P =>
                {
                    var aReceiverTranslateInfo = ChatAutoTranslatesDB.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == P.entityId);
                    if (TranslationNeeded(aSender, aSenderTranslateInfo, P, aReceiverTranslateInfo, aInfo)) {
                        var TranslateText = TranslateAPI.Translate(
                            aSenderTranslateInfo   == null ? ChatAutoTranslatesDB.Configuration.DefaultSourceLanguage : aSenderTranslateInfo  .SelectedLanguage,
                            aReceiverTranslateInfo == null ? ChatAutoTranslatesDB.Configuration.ServerMainLanguage    : aReceiverTranslateInfo.SelectedLanguage,
                            aInfo.msg, ref Cache);

                        if (TranslateText != null)
                        {
                            log($"**HandleEmpyrionChatAutoTranslate SendTranslate {aSenderTranslateInfo?.PlayerName}:{aSenderTranslateInfo?.PlayerId} to {aReceiverTranslateInfo?.PlayerName}:{aReceiverTranslateInfo?.PlayerId} clientId:{P.clientId} -> {aSenderTranslateInfo?.SelectedLanguage}", LogLevel.Debug);
                            Request_InGameMessage_SinglePlayer($"{aSender.playerName}:{TranslateText}".ToIdMsgPrio(P.entityId, MessagePriorityType.Info, 10), null, E => log($"SendTranslateToSinglePlayer: {P.playerName} -> {E}", LogLevel.Debug));
                        }
                    }
                }));
            });
        }

        private bool TranslationNeeded(PlayerInfo aSender, ChatAutoTranslateDB.TranslationSettings aSenderTranslateInfo, PlayerInfo aReceiver, ChatAutoTranslateDB.TranslationSettings aReceiverTranslateInfo, ChatInfo aInfo)
        {
            if (aReceiverTranslateInfo == null && aSenderTranslateInfo == null) return false; // beide in Serversprache unterwegs

            var SameFactionOrGlobal = aInfo.type == (byte)ChatType.Global || aReceiver.factionId == aSender.factionId;
            if (!SameFactionOrGlobal) return false; // Chat nicht für Spieler bestimmt

            if (aReceiverTranslateInfo != null && aSenderTranslateInfo == null) return true;  // Unterschiedliche Spracheinstellungen
            if (aReceiverTranslateInfo == null && aSenderTranslateInfo != null) return true;  // Unterschiedliche Spracheinstellungen

            return aReceiverTranslateInfo.SelectedLanguage != aSenderTranslateInfo.SelectedLanguage;
        }

        private void InitializeDBFileWatcher()
        {
            DBFileChangedWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ChatAutoTranslatesDBFilename),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(ChatAutoTranslatesDBFilename)
            };
            DBFileChangedWatcher.Changed += (s, e) => ChatAutoTranslatesDB = ChatAutoTranslateDB.ReadDB(ChatAutoTranslatesDBFilename);
            DBFileChangedWatcher.EnableRaisingEvents = true;
        }

        private void InitializeDB()
        {
            ChatAutoTranslatesDBFilename = Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Mods\EmpyrionChatAutoTranslate\ChatAutoTranslatesDB.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(ChatAutoTranslatesDBFilename));

            ChatAutoTranslateDB.LogDB = log;
            ChatAutoTranslatesDB = ChatAutoTranslateDB.ReadDB(ChatAutoTranslatesDBFilename);
            ChatAutoTranslatesDB.SaveDB(ChatAutoTranslatesDBFilename);

            TranslateAPI.TranslateServiceUrl = ChatAutoTranslatesDB.Configuration.TranslateServiceUrl;
            TranslateAPI.TanslateRespose     = ChatAutoTranslatesDB.Configuration.TanslateRespose;
        }


        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private void ExecCommand(SubCommand aCommand, ChatInfo info, Dictionary<string, string> args)
        {
            log($"**HandleEmpyrionChatAutoTranslate {info.type}#{aCommand}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}", LogLevel.Message);

            if (info.type != (byte)ChatType.Faction) return;

            switch (aCommand)
            {
                case SubCommand.Help    : DisplayHelp               (info.playerId); break;
                case SubCommand.Set     : SetTranslation            (info.playerId, args["language"]); break;
                case SubCommand.Box     : Translate                 (info.playerId, args["text"]); break;
                case SubCommand.Clear   : ClearTranslation          (info.playerId); break;
                case SubCommand.ListAll : ListAllChatAutoTranslates (info.playerId); break;
            }
        }

        private void Translate(int aPlayerId, string aText)
        {
            Request_Player_Info(aPlayerId.ToId(), P =>
            {
                var TargetLanguage = GetTargetLanguage(P.entityId);
                ShowDialog(aPlayerId, P, 
                    TranslateAPI.Translate("de",   TargetLanguage, "Automatische Übersetzung"), 
                    TranslateAPI.Translate("auto", TargetLanguage, aText));
            });
        }

        private string GetTargetLanguage(int aPlayerEntityId)
        {
            return ChatAutoTranslatesDB.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == aPlayerEntityId)?.SelectedLanguage 
                ?? ChatAutoTranslatesDB.Configuration.ServerMainLanguage;
        }

        private void SetTranslation(int aPlayerId, string aLanguage)
        {
            Request_Player_Info(aPlayerId.ToId(), P =>
            {
                var data = ChatAutoTranslatesDB.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == P.entityId);
                if (data == null) ChatAutoTranslatesDB.PlayerTranslationSettings.Add(data = new ChatAutoTranslateDB.TranslationSettings()
                {
                    PlayerId   = P.entityId,
                    PlayerName = P.playerName,
                });

                data.SelectedLanguage = aLanguage;
                SaveTranslationDB();
                ShowDialog(aPlayerId, P, TranslateAPI.Translate("de", aLanguage, "Automatische Übersetzung"), TranslateAPI.Translate("de", aLanguage, "Automatische Übersetzung ist eingestellt auf: ") + aLanguage);
            });
        }

        private void ClearTranslation(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), P =>
            {
                ChatAutoTranslatesDB.PlayerTranslationSettings = ChatAutoTranslatesDB.PlayerTranslationSettings.Where(T => T.PlayerId != P.entityId).ToList();
                SaveTranslationDB();
                ShowDialog(aPlayerId, P, TranslateAPI.Translate("de", ChatAutoTranslatesDB.Configuration.ServerMainLanguage, "Automatische Übersetzung"), TranslateAPI.Translate("de", ChatAutoTranslatesDB.Configuration.ServerMainLanguage, "Automatische Übersetzung ist eingestellt auf Serversprache: " + ChatAutoTranslatesDB.Configuration.ServerMainLanguage));
            });
        }

        private void SaveTranslationDB()
        {
            DBFileChangedWatcher.EnableRaisingEvents = false;
            ChatAutoTranslatesDB.SaveDB(ChatAutoTranslatesDBFilename);
            DBFileChangedWatcher.EnableRaisingEvents = true;
        }

        private void ListAllChatAutoTranslates(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), (P) =>
            {
                ShowDialog(aPlayerId, P, $"ChatAutoTranslates", ChatAutoTranslatesDB.PlayerTranslationSettings.OrderBy(T => T.PlayerName).Aggregate("\n", (S, T) => S + T.ToFormatedString() + "\n"));
            });
        }

        private void LogError(string aPrefix, ErrorInfo aError)
        {
            log($"{aPrefix} Error: {aError.errorType} {aError.ToString()}", LogLevel.Error);
        }

        private int getIntParam(Dictionary<string, string> aArgs, string aParameterName)
        {
            string valueStr;
            if (!aArgs.TryGetValue(aParameterName, out valueStr)) return 0;

            int value;
            if (!int.TryParse(valueStr, out value)) return 0;

            return value;
        }

        void ShowDialog(int aPlayerId, PlayerInfo aPlayer, string aTitle, string aMessage)
        {
            Request_ShowDialog_SinglePlayer(new DialogBoxData()
            {
                Id      = aPlayerId,
                MsgText = $"{aTitle}: [c][ffffff]{aPlayer.playerName}[-][/c] with permission [c][ffffff]{(PermissionType)aPlayer.permission}[-][/c]\n" + aMessage,
            });
        }

        private void DisplayHelp(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), (P) =>
            {
                var CurrentAssembly = Assembly.GetAssembly(this.GetType());
                //[c][hexid][-][/c]    [c][019245]test[-][/c].

                ShowDialog(aPlayerId, P, "Commands",
                    "\n" + String.Join("\n", GetChatCommandsForPermissionLevel((PermissionType)P.permission).Select(C => C.MsgString()).ToArray()) +
                    $"\n\n[c][c0c0c0]{CurrentAssembly.GetAttribute<AssemblyTitleAttribute>()?.Title} by {CurrentAssembly.GetAttribute<AssemblyCompanyAttribute>()?.Company} Version:{CurrentAssembly.GetAttribute<AssemblyFileVersionAttribute>()?.Version}[-][/c]"
                    );
            });
        }

    }
}
