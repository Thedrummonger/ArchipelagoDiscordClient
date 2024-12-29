using Discord.Net;
using Discord.WebSocket;
using Discord;
using ArchipelagoDiscordClient.Commands;

namespace ArchipelagoDiscordClient.Services
{
	public class DiscordCommandRegistrationService : IDiscordCommandRegistrationService
	{
		private readonly DiscordSocketClient _client;
        private readonly IEnumerable<ICommand> _commands;

        public DiscordCommandRegistrationService(DiscordSocketClient client, IEnumerable<ICommand> commands)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_commands = commands;

        }

		public async Task RegisterCommandsAsync()
		{
			foreach(var command in _commands)
            {
                Console.WriteLine($"registering {command.CommandName}");
				try
                {
                    await _client.CreateGlobalApplicationCommandAsync(command.Properties);
                }
                catch (HttpException ex)
                {
                    Console.WriteLine($"Error registering commands: {ex.Message}");
                }
            }
		}
	}
}
