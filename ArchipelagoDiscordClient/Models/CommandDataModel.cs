using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClient.Models
{
    public class CommandDataModel
    {
        public required ulong guildId { get; set; }
        public required ulong channelId { get; set; }
        public string? channelName { get; set; }
        public SocketTextChannel? socketTextChannel { get; set; }
        public Dictionary<string, SocketSlashCommandDataOption> Arguments { get; set; } = [];
        public SocketSlashCommandDataOption? GetArg(string key)
        {
            if (!Arguments.TryGetValue(key, out var arg)) { return null; }
            return arg;
        }
    }
}
