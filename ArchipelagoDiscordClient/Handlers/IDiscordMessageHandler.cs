using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Handlers
{
	public interface IDiscordMessageHandler
	{
		Task HandleMessageReceivedAsync(SocketMessage message);
	}
}