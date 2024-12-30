using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Models;

namespace ArchipelagoDiscordClient.Services
{
    public interface IArchipelagoSessionService
    {
        void CreateSession(CreateSessionModel model);

        Task RemoveSessionAsync(ulong guildId, ulong channelId);

        ArchipelagoSession? GetActiveSessionByChannelIdAsync(ulong guildId, ulong channelId);

        Dictionary<ulong, ArchipelagoSession> GetActiveSessionsByGuildIdAsync(ulong guildId);
    }
}