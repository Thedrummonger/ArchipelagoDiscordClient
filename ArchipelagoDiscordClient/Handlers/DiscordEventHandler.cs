using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ArchipelagoDiscordClient.Handlers
{
    public class DiscordEventHandler : IDiscordEventHandler
	{
		private readonly DiscordSocketClient _client;
		private readonly IDiscordCommandRegistrationService _commandRegistrationService;
		private readonly IServiceProvider _serviceProvider;

		public DiscordEventHandler(
			DiscordSocketClient client, 
			IDiscordCommandRegistrationService commandRegistrationService,
			IServiceProvider serviceProvider)
		{
			_client = client;
			_commandRegistrationService = commandRegistrationService;
			_serviceProvider = serviceProvider;
		}

		public void SubscribeToDiscordEvents()
		{
			_client.Log += LogAsync;
			_client.Ready += ReadyAsync;
			_client.SlashCommandExecuted += async command =>
			{
				var slashCommandHandler = _serviceProvider.GetRequiredService<ICommandService>();
				await slashCommandHandler.ExecuteAsync(command);
			};

			_client.MessageReceived += async message =>
			{
				var discordMessageHandler = _serviceProvider.GetRequiredService<IDiscordMessageHandler>();
				await discordMessageHandler.HandleMessageReceivedAsync(message);
			};
		}

		private Task LogAsync(LogMessage logMessage)
		{
			Console.WriteLine(logMessage);
			return Task.CompletedTask;
		}

		private async Task ReadyAsync()
		{
			await _commandRegistrationService.RegisterCommandsAsync();
			Console.WriteLine("Bot is ready!");
		}
	}
}
