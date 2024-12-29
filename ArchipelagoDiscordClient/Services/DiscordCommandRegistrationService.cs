using Discord.Net;
using Discord.WebSocket;
using Discord;

namespace ArchipelagoDiscordClient.Services
{
	public class DiscordCommandRegistrationService : IDiscordCommandRegistrationService
	{
		private readonly DiscordSocketClient _client;

		public DiscordCommandRegistrationService(DiscordSocketClient client)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
		}

		public async Task RegisterCommandsAsync()
		{
			// Register commands globally
			var connectCommand = new SlashCommandBuilder()
				.WithName("connect")
				.WithDescription("Connect this channel to an Archipelago server")
				.AddOption("ip", ApplicationCommandOptionType.String, "Server IP", true)
				.AddOption("port", ApplicationCommandOptionType.Integer, "Server Port", true)
				.AddOption("game", ApplicationCommandOptionType.String, "Game name", true)
				.AddOption("name", ApplicationCommandOptionType.String, "Player name", true)
				.AddOption("password", ApplicationCommandOptionType.String, "Optional password", false)
				.Build();

			var disconnectCommand = new SlashCommandBuilder()
				.WithName("disconnect")
				.WithDescription("Disconnect this channel from the Archipelago server")
				.Build();

			var showSessionsCommand = new SlashCommandBuilder()
				.WithName("show_sessions")
				.WithDescription("Show all active Archipelago sessions in this server")
				.Build();

			var showChannelSessionCommand = new SlashCommandBuilder()
				.WithName("show_channel_session")
				.WithDescription("Show the active Archipelago session for this channel")
				.Build();

			var showHintsCommand = new SlashCommandBuilder()
				.WithName("show_hints")
				.WithDescription("Shows all hint for the current player")
				.Build();

			try
			{
				await _client.CreateGlobalApplicationCommandAsync(connectCommand);
				await _client.CreateGlobalApplicationCommandAsync(disconnectCommand);
				await _client.CreateGlobalApplicationCommandAsync(showSessionsCommand);
				await _client.CreateGlobalApplicationCommandAsync(showChannelSessionCommand);
				await _client.CreateGlobalApplicationCommandAsync(showHintsCommand);
			}
			catch (HttpException ex)
			{
				Console.WriteLine($"Error registering commands: {ex.Message}");
			}
		}
	}
}
