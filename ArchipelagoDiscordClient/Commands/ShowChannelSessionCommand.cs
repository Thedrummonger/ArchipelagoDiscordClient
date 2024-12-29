using Archipelago.MultiClient.Net;
using System.Collections.Concurrent;
using Discord.WebSocket;
using Discord;
using ArchipelagoDiscordClient.Constants;

namespace ArchipelagoDiscordClient.Commands
{
	public class ShowChannelSessionCommand : ICommand
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public string CommandName => CommandTypes.ShowChannelSessionCommand;

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Show the active Archipelago session for this channel")
                .Build();

        public ShowChannelSessionCommand(
			Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache)
		{
			_activeSessions = activeSessions;
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

			// Check if the channel has an active session
			if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session))
			{
				await command.RespondAsync("No active Archipelago session in this channel.", ephemeral: true);
				return;
			}

			// Build the response
			var response = $"**Active Archipelago Session**\n" +
						   $"  **Server**: {session.Socket.Uri}\n" +
						   $"  **Player**: {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}({session.ConnectionInfo.Slot})\n";

			await command.RespondAsync(response, ephemeral: true);
		}
	}
}
