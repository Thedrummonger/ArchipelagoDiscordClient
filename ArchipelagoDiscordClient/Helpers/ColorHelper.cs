using ArchipelagoDiscordClient.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClient.Helpers
{
    public static class ColorHelper
    {
        //Using discords Code Markdown we can color words in messages
        //Uses this method https://gist.github.com/kkrypt0nn/a02506f3712ff2d1c8ca7c9e0aed7c06
        //The colors don't match 100% with archipelago but we can get close enough

        public static string SetColor(this string input, Archipelago.MultiClient.Net.Models.Color color)
        {
            if (!ColorCodes.ColorCodeDict.TryGetValue(color, out Tuple<string, string> Parts)) { return input; }
            return $"{Parts.Item1}{input}{Parts.Item2}";
        }
    }
}
