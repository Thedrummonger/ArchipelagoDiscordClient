using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
	public interface ICommand
	{
		Task ExecuteAsync(SocketSlashCommand command);
		string CommandName { get; }
	}
}
