using System.Collections.Concurrent;
using Discord.WebSocket;
using Discord;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Handlers;

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
			Utility.GetCommandData(command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel);
			if (socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}
			_channelCache.TryAdd(channelId, socketTextChannel);

			var session = _sessionService.GetActiveSessionByChannelIdAsync(guildId, channelId);
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
