using System.Collections.Concurrent;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Services
{
	public class ChannelService : IChannelService
	{
		private readonly DiscordSocketClient _client;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public ChannelService(
			DiscordSocketClient client,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache)
		{
			_client = client;
			_channelCache = channelCache;
		}

		public SocketTextChannel? GetChannel(ulong id)
		{
			if (!_channelCache.TryGetValue(id, out SocketTextChannel? channel))
			{
				SocketTextChannel? fetchedChannel = null;
				if (_client.GetChannel(id) is SocketTextChannel textChannel)
				{
					fetchedChannel = textChannel;
				}

				_channelCache.TryAdd(id, fetchedChannel);
				return fetchedChannel;
			}

			return channel;
		}
	}
}
