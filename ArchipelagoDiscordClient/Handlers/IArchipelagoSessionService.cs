using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Models;

namespace ArchipelagoDiscordClient.Handlers
{
	public interface IArchipelagoSessionService
	{
		void CreateSession(CreateSessionModel model);

		bool TryGetSession(ulong guildId, ulong channelId, out ArchipelagoSession? session);

		Task RemoveSessionAsync(ulong guildId, ulong channelId);

		ArchipelagoSession? GetActiveSessionByChannelIdAsync(ulong guildId, ulong channelId);

		Dictionary<ulong, ArchipelagoSession> GetActiveSessionsByGuildAsync(ulong guildId);
	}
}