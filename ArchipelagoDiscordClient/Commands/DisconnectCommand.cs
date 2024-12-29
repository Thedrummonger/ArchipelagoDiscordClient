using System.Collections.Concurrent;
using Archipelago.MultiClient.Net;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
	public class DisconnectCommand : ICommand
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;

		public string CommandName => "disconnect";

		public DisconnectCommand(Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions)
		{
			_activeSessions = activeSessions;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
			Utility.GetCommandData(command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel);

			if (socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}

			if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.ContainsKey(channelId))
			{
				await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
				return;
			}

			await CleanAndCloseChannel(guildId, channelId);
			await command.RespondAsync($"Successfully disconnected {channelName} from Archipelago.");
		}

		private async Task CleanAndCloseChannel(ulong guildId, ulong channelId)
		{
			if (_activeSessions[guildId].TryGetValue(channelId, out var session))
			{
				await session.Socket.DisconnectAsync();
				_activeSessions[guildId].Remove(channelId);
			}
		}
	}
}
