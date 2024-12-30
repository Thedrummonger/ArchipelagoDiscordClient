using System.Collections.Concurrent;
using Discord.WebSocket;
using Discord;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Helpers;
using ArchipelagoDiscordClient.Services;

namespace ArchipelagoDiscordClient.Commands
{
    public class ShowChannelSessionCommand : ICommand
	{
		private readonly IArchipelagoSessionService _sessionService;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public string CommandName => CommandTypes.ShowChannelSessionCommand;

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Show the active Archipelago session for this channel")
                .Build();

        public ShowChannelSessionCommand(
			IArchipelagoSessionService sessionService,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache)
		{
			_sessionService = sessionService;
			_channelCache = channelCache;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
            var commandData = command.GetCommandData();
			if (commandData.socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}
			_channelCache.TryAdd(commandData.channelId, commandData.socketTextChannel);

			var session = _sessionService.GetActiveSessionByChannelIdAsync(commandData.guildId, commandData.channelId);
			if (session is null)
			{
				await command.RespondAsync("No active Archipelago session in this channel.", ephemeral: true);
				return;
			}

			var response = $"**Active Archipelago Session**\n" +
						   $"  **Server**: {session.Socket.Uri}\n" +
						   $"  **Player**: {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}({session.ConnectionInfo.Slot})\n";

			await command.RespondAsync(response, ephemeral: true);
		}
	}
}
