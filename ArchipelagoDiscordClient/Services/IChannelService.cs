using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Services
{
	public interface IChannelService
	{
		SocketTextChannel? GetChannel(ulong id);
	}
}