using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using EmpyrionNetAPIDefinitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    public partial class EmpyrionChatAutoTranslate : EmpyrionModBase
    {
        public ModGameAPI GameAPI { get; set; }
        public ConfigurationManager<Configuration> Configuration { get; set; }

        enum SubCommand
        {
            Help,
            Set,
            Box,
            Clear,
            ListAll
        }

        public EmpyrionChatAutoTranslate()
        {
            EmpyrionConfiguration.ModName = "EmpyrionChatAutoTranslate";
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;
            TranslateAPI.LogDB = (S, L) => Log(S, L);

            Log($"**HandleEmpyrionChatAutoTranslate loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

            InitializeDB();
            LogLevel = Configuration.Current.LogLevel;
            ChatCommandManager.CommandPrefix = Configuration.Current.CommandPrefix;

            Event_ChatMessage += (C) =>
            {
                try
                {
                    Task.Run(() => EmpyrionChatAutoTranslate_Event_ChatMessage(C));
                }
                catch (Exception error)
                {
                    Log($"ChatAutoTranslate_Event_ChatMessage: {error}", LogLevel.Error);
                }
            };

            ChatCommands.Add(new ChatCommand(@"trans help",                    (I, A) => ExecCommand(SubCommand.Help,     I, A), "Show the help"));
            ChatCommands.Add(new ChatCommand(@"trans set (?<language>.*)",     (I, A) => ExecCommand(SubCommand.Set,      I, A), "Set the translation language"));
            ChatCommands.Add(new ChatCommand(@"trans box (?<text>.+)",         (I, A) => ExecCommand(SubCommand.Box,      I, A), "Translate to a messagebox"));
            ChatCommands.Add(new ChatCommand(@"trans clear",                   (I, A) => ExecCommand(SubCommand.Clear,    I, A), "Back to the Serverlanguage"));
            ChatCommands.Add(new ChatCommand(@"trans listall",                 (I, A) => ExecCommand(SubCommand.ListAll,  I, A), "List all translation settings", PermissionType.Moderator));
        }

        private async Task EmpyrionChatAutoTranslate_Event_ChatMessage(ChatInfo info)
        {
            var UpperMsg = info.msg.ToUpper();
            if (Configuration.Current.SupressTranslatePrefixes.Any(M => UpperMsg.StartsWith(M))) return;

            Log($"**HandleEmpyrionChatAutoTranslate Translate {info.type}: playerId:{info.playerId} recipientEntityId:{info.recipientEntityId} recipientFactionId:{info.recipientFactionId} msg:{info.msg}", LogLevel.Message);

            var P = await Request_Player_Info(info.playerId.ToId());
            await SendTranslateToSinglePlayer(P, info);
        }

        private async Task SendTranslateToSinglePlayer(PlayerInfo aSender, ChatInfo aInfo)
        {
            if (aInfo.msg.Where(Char.IsLetter).Count() < Configuration.Current.TranslateMinTextLength) return;

            var Cache = new Dictionary<string, string>();

            var aSenderTranslateInfo = Configuration.Current.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == aSender.entityId);
            Log($"**HandleEmpyrionChatAutoTranslate Translate found {aSenderTranslateInfo?.PlayerName}:{aSenderTranslateInfo?.PlayerId} playerId:{aInfo.playerId}-> {aSenderTranslateInfo?.SelectedLanguage}", LogLevel.Message);

            var L = await Request_Player_List();

            L.list.ForEach(async PI => await SendTranslateToEveryPlayer(PI, aSender, aInfo, Cache, aSenderTranslateInfo));
        }

        private async Task SendTranslateToEveryPlayer(int requestPlayerId, PlayerInfo aSender, ChatInfo aInfo, Dictionary<string, string> Cache, TranslationSettings aSenderTranslateInfo)
        {
            var P = await Request_Player_Info(requestPlayerId.ToId());
            var aReceiverTranslateInfo = Configuration.Current.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == P.entityId);

            if (TranslationNeeded(aSender, aSenderTranslateInfo, P, aReceiverTranslateInfo, aInfo))
            {
                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        var TranslateText = TranslateAPI.Translate(
                            aSenderTranslateInfo == null ? Configuration.Current.DefaultSourceLanguage : aSenderTranslateInfo.SelectedLanguage,
                            aReceiverTranslateInfo == null ? Configuration.Current.ServerMainLanguage : aReceiverTranslateInfo.SelectedLanguage,
                            aInfo.msg, Cache, Configuration.Current.TranslateDelayTime);

                        if (TranslateText != null)
                        {
                            Log($"**HandleEmpyrionChatAutoTranslate SendTranslate {aSenderTranslateInfo?.PlayerName}:{aSenderTranslateInfo?.PlayerId} to {aReceiverTranslateInfo?.PlayerName}:{aReceiverTranslateInfo?.PlayerId} clientId:{P.clientId} -> {aSenderTranslateInfo?.SelectedLanguage}", LogLevel.Debug);
                            Task.Run(() => Request_InGameMessage_SinglePlayer($"[c]{(aInfo.type == (byte)ChatType.Faction ? "[00ff00]" : "[ff00ff]")}{aSender.playerName}[/c]: [c][ffffff]{TranslateText}[/c]".ToIdMsgPrio(P.entityId, MessagePriorityType.Info, Configuration.Current.TranslateDisplayTime)));
                        }
                    }
                    catch (Exception error)
                    {
                        Log($"SendTranslateToEveryPlayer: {error}", LogLevel.Error);
                    }
                })).Start();
            }
        }

        private bool TranslationNeeded(PlayerInfo aSender, TranslationSettings aSenderTranslateInfo, PlayerInfo aReceiver, TranslationSettings aReceiverTranslateInfo, ChatInfo aInfo)
        {
            if (aReceiverTranslateInfo == null && aSenderTranslateInfo == null) return false; // beide in Serversprache unterwegs

            var SameFactionOrGlobal = aInfo.type == (byte)ChatType.Global || aReceiver.factionId == aSender.factionId;
            if (!SameFactionOrGlobal) return false; // Chat nicht für Spieler bestimmt

            if (aReceiverTranslateInfo != null && aSenderTranslateInfo == null) return true;  // Unterschiedliche Spracheinstellungen
            if (aReceiverTranslateInfo == null && aSenderTranslateInfo != null) return true;  // Unterschiedliche Spracheinstellungen

            return aReceiverTranslateInfo.SelectedLanguage != aSenderTranslateInfo.SelectedLanguage;
        }

        private void InitializeDB()
        {
            Configuration = new ConfigurationManager<Configuration>() {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json")
            };

            Configuration.Load();
            Configuration.Save();

            TranslateAPI.TranslateServiceUrl = Configuration.Current.TranslateServiceUrl;
            TranslateAPI.TanslateRespose     = Configuration.Current.TanslateRespose;
        }


        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private async Task ExecCommand(SubCommand aCommand, ChatInfo info, Dictionary<string, string> args)
        {
            try
            {
                Log($"**HandleEmpyrionChatAutoTranslate {info.type}#{aCommand}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}", LogLevel.Message);

                if (info.type != (byte)ChatType.Faction) return;

                switch (aCommand)
                {
                    case SubCommand.Help    : await DisplayHelp               (info.playerId, ""); break;
                    case SubCommand.Set     : await SetTranslation            (info.playerId, args["language"]); break;
                    case SubCommand.Box     : await Translate                 (info.playerId, args["text"]); break;
                    case SubCommand.Clear   : await ClearTranslation          (info.playerId); break;
                    case SubCommand.ListAll : await ListAllChatAutoTranslates (info.playerId); break;
                }
            }
            catch (Exception error)
            {
                Log($"ExecCommand: {error}", LogLevel.Error);
            }
        }

        private async Task Translate(int aPlayerId, string aText)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());
            var TargetLanguage = GetTargetLanguage(P.entityId);
            await ShowDialog(aPlayerId, P, "Empyrion Chat Auto Translate", TranslateAPI.Translate("auto", TargetLanguage, aText));
        }

        private string GetTargetLanguage(int aPlayerEntityId)
        {
            return Configuration.Current.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == aPlayerEntityId)?.SelectedLanguage 
                ?? Configuration.Current.ServerMainLanguage;
        }

        private async Task SetTranslation(int aPlayerId, string aLanguage)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());
            var data = Configuration.Current.PlayerTranslationSettings.FirstOrDefault(T => T.PlayerId == P.entityId);
            if (data == null) Configuration.Current.PlayerTranslationSettings.Add(data = new TranslationSettings()
            {
                PlayerId   = P.entityId,
                PlayerName = P.playerName,
            });

            data.SelectedLanguage = aLanguage;
            Configuration.Save(); ;
            await ShowDialog(aPlayerId, P, "Empyrion Chat Auto Translate", TranslateAPI.Translate("de", aLanguage, "Automatische Übersetzung ist eingestellt auf:") + " " + aLanguage);
        }

        private async Task ClearTranslation(int aPlayerId)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());

            Configuration.Current.PlayerTranslationSettings = Configuration.Current.PlayerTranslationSettings.Where(T => T.PlayerId != P.entityId).ToList();
            Configuration.Save();
            await ShowDialog(aPlayerId, P, "Empyrion Chat Auto Translate", TranslateAPI.Translate("de", Configuration.Current.ServerMainLanguage, "Automatische Übersetzung ist eingestellt auf:") + " " + Configuration.Current.DefaultSourceLanguage);
        }

        private async Task ListAllChatAutoTranslates(int aPlayerId)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());

            await ShowDialog(aPlayerId, P, $"ChatAutoTranslates", Configuration.Current.PlayerTranslationSettings.OrderBy(T => T.PlayerName).Aggregate("\n", (S, T) => S + T.ToFormatedString() + "\n"));
        }

        private void LogError(string aPrefix, ErrorInfo aError)
        {
            Log($"{aPrefix} Error: {aError.errorType} {aError.ToString()}", LogLevel.Error);
        }

        private int getIntParam(Dictionary<string, string> aArgs, string aParameterName)
        {
            string valueStr;
            if (!aArgs.TryGetValue(aParameterName, out valueStr)) return 0;

            int value;
            if (!int.TryParse(valueStr, out value)) return 0;

            return value;
        }

    }
}
