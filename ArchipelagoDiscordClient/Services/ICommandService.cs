using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Services
{
	public interface ICommandService
	{
		Task ExecuteAsync(SocketSlashCommand command);
	}
}