using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
	public class ShowSessionsCommand : ICommand
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly IChannelService _channelService;

		public string CommandName => "show_sessions";


        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName("show_sessions")
                .WithDescription("Show all active Archipelago sessions in this server")
                .Build();

        public ShowSessionsCommand(
			Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions,
			IChannelService channelService)
		{
			_activeSessions = activeSessions;
			_channelService = channelService;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
			var guildId = command.GuildId ?? 0;
			if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || guildSessions.Count == 0)
			{
				await command.RespondAsync("No active Archipelago sessions in this guild.", ephemeral: true);
				return;
			}

			var response = "Active Archipelago Sessions:\n";
			foreach (var (channelId, session) in guildSessions)
			{
				var channel = _channelService.GetChannel(channelId);
				if (channel == null) continue;

				response += $"- **Channel**: {channel.Name}\n" +
							$"  **Server**: {session.Socket.Uri}\n" +
							$"  **Player**: {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}({session.ConnectionInfo.Slot})\n";
			}

			await command.RespondAsync(response, ephemeral: true);
		}
	}
}
