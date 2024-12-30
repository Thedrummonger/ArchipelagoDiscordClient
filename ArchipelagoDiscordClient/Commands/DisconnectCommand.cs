using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Helpers;
using ArchipelagoDiscordClient.Services;
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
            var commandData = command.GetCommandData();
			if (commandData.socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}

			Console.WriteLine($"Disconnecting from {commandData.channelName} from Archipelago");
			await command.RespondAsync($"Disconnecting from {commandData.channelName} from Archipelago...");

			try
			{
				await _sessionService.RemoveSessionAsync(commandData.guildId, commandData.channelId);
			}
			catch (Exception ex)
			{
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to disconnect: {ex.Message}");
			}

			await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully disconnected {commandData.channelName} from Archipelago.");
		}
	}
}
