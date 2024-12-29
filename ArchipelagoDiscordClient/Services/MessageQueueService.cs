using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Services
{
	public class MessageQueueService : IMessageQueueService
	{
		private readonly Dictionary<SocketTextChannel, List<string>> _sendQueue = new();

		public void QueueMessage(SocketTextChannel channel, string message)
		{
			if (!_sendQueue.ContainsKey(channel))
			{
				_sendQueue[channel] = new List<string>();
			}
			_sendQueue[channel].Add(message);
		}

		public Dictionary<SocketTextChannel, List<string>> GetSendQueue()
		{
			return _sendQueue;
		}

		public void ClearQueue(SocketTextChannel channel)
		{
			if (_sendQueue.ContainsKey(channel))
			{
				_sendQueue[channel].Clear();
			}
		}
	}
}
