using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClient
{
    internal class Utility
    {
        public static bool IsClientNotification(string message, out Version version)
        {
            string pattern = @"Client\((\d+\.\d+\.\d+)\)";
            version = new Version(0, 0, 0);
            Match match = Regex.Match(message, pattern);
            if (match.Success)
            {
                string versionString = match.Groups[1].Value;
                if (Version.TryParse(versionString, out Version? ParsedVersion))
                    version = ParsedVersion;
                return true;
            }
            return false;
        }
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
            //{ Archipelago.MultiClient.Net.Models.Color.White, new (@"[2;37m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.SlateBlue, new (@"[2;34m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Salmon, new (@"[2;33m", @"[0m") },
            { Archipelago.MultiClient.Net.Models.Color.Plum, new (@"[2;35m", @"[0m") }
        };
    }
}
