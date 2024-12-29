using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace ArchipelagoDiscordClient
{
    internal class Utility
    {
        //Checks if the AP message is a client notification
        //Mostly messages about a client connecting or disconnecting
        public static bool IsClientNotification(string message, out Version version)
        {
            string pattern = @"Client\((\d+\.\d+\.\d+)\)"; //Check for string with the client version info.
                                                           //This is consistent for all client connection messages
                                                           //We can assume if it has this string, it a client message
            version = new Version(0, 0, 0);
            Match match = Regex.Match(message, pattern);
            if (match.Success)
            {
                string versionString = match.Groups[1].Value; //Pull out the version from the client info string, currently unused.
                if (Version.TryParse(versionString, out Version? ParsedVersion))
                    version = ParsedVersion;
                return true;
            }
            return false;
        }

        //Probably doesn't need a dedicated function, but this may very well expand in scope in the future
        public static void GetCommandData(SocketSlashCommand command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel)
        {
            guildId = command.GuildId ?? 0;
            channelId = command.Channel.Id;
            channelName = command.Channel?.Name ?? channelId.ToString();
            socketTextChannel = command.Channel is SocketTextChannel STC ? STC : null;
        }


        //Using discords Code Markdown we can color words in messages
        //Uses this method https://gist.github.com/kkrypt0nn/a02506f3712ff2d1c8ca7c9e0aed7c06
        //The colors don't match 100% with archipelago but we can get close enough

        public static string ColorString(string input, Archipelago.MultiClient.Net.Models.Color color)
        {
            if (!ColorCodes.TryGetValue(color, out Tuple<string, string> Parts)) { return input; }
            return $"{Parts.Item1}{input}{Parts.Item2}";
        }

        static readonly Dictionary<Archipelago.MultiClient.Net.Models.Color, Tuple<string, string>> ColorCodes = new()
        {
            { Archipelago.MultiClient.Net.Models.Color.Red, new (@"[2;31m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Green, new (@"[2;32m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Yellow, new (@"[2;33m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Blue, new (@"[2;34m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Magenta, new (@"[2;35m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Cyan, new (@"[2;36m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Black, new (@"[2;30m", @"[0m") },
            //{ Archipelago.MultiClient.Net.Models.Color.White, new (@"[2;37m", @"[0m") }, //Uncolored discord messages are already white
            { Archipelago.MultiClient.Net.Models.Color.SlateBlue, new (@"[2;34m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Salmon, new (@"[2;33m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Plum, new (@"[2;35m", @"[0m") }
        };
    }
}
