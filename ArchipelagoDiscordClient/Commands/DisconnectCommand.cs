using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Handlers;
using Discord;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
	public class DisconnectCommand : ICommand
	{
		private readonly IArchipelagoSessionService _sessionService;

		public string CommandName => CommandTypes.DisconnectCommand;

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Disconnect this channel from the Archipelago server")
                .Build();

        public DisconnectCommand(IArchipelagoSessionService sessionService)
		{
			_sessionService = sessionService;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
			Utility.GetCommandData(command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel);
			if (socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}

			Console.WriteLine($"Disconnecting from {channelName} from Archipelago");
			await command.RespondAsync($"Disconnecting from {channelName} from Archipelago...");

			try
			{
				await _sessionService.RemoveSessionAsync(guildId, channelId);
			}
			catch (Exception ex)
			{
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to disconnect: {ex.Message}");
			}

			await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully disconnected {channelName} from Archipelago.");
		}
	}
}
