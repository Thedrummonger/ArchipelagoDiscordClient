using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Services
{
	public interface IMessageQueueService
	{
		void ClearQueue(SocketTextChannel channel);

		Dictionary<SocketTextChannel, List<string>> GetSendQueue();

		void QueueMessage(SocketTextChannel channel, string message);
	}
}