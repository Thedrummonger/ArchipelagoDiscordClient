using ArchipelagoDiscordClient.Models;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClient.Helpers
{
    public static class DiscordCommandHelper
    {

        //Probably doesn't need a dedicated function, but this may very well expand in scope in the future
        public static CommandDataModel GetCommandData(this SocketSlashCommand command)
        {
            var Data = new CommandDataModel()
            {
                guildId = command.GuildId ?? 0,
                channelId = command.ChannelId ?? 0,
                channelName = command.Channel?.Name,
                socketTextChannel = command.Channel is SocketTextChannel STC ? STC : null
            };
            Data.channelName ??= Data.channelId.ToString();
            foreach(var i in command.Data.Options)
            {
                Data.Arguments[i.Name] = i;
            }
            return Data;
        }
    }
}
