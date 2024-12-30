using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
    public class ShowSessionsCommand : ICommand
	{
		private readonly IArchipelagoSessionService _sessionService;
		private readonly IChannelService _channelService;

		public string CommandName => CommandTypes.ShowSessionsCommand;

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Show all active Archipelago sessions in this server")
                .Build();

        public ShowSessionsCommand(
			IArchipelagoSessionService sessionService,
			IChannelService channelService)
		{
			_sessionService = sessionService;
			_channelService = channelService;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
			var guildId = command.GuildId ?? 0;
			var guildSessions = _sessionService.GetActiveSessionsByGuildIdAsync(guildId);
			if (guildSessions.Count == 0)
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
