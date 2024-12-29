using ArchipelagoDiscordClient.Commands;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Services
{
	public class CommandService : ICommandService
	{
		private readonly Dictionary<string, ICommand> _commands = new();

		public CommandService(IEnumerable<ICommand> commands)
		{
			foreach (var command in commands)
			{
				_commands.Add(command.CommandName, command);
			}
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
			if (_commands.TryGetValue(command.Data.Name.ToLower(), out var strategy))
			{
				await strategy.ExecuteAsync(command);
			}
			else
			{
				await command.RespondAsync("Unknown command.", ephemeral: true);
			}
		}
	}
}
