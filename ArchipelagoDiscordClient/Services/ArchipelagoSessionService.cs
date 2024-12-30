using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClient.Handlers;
using ArchipelagoDiscordClient.Helpers;
using ArchipelagoDiscordClient.Models;
using ArchipelagoDiscordClient.Settings;
using Microsoft.Extensions.Options;

namespace ArchipelagoDiscordClient.Services
{
	public class ArchipelagoSessionService : IArchipelagoSessionService
    {
        private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly BotSettings _settings;

		private readonly IMessageQueueService _messageQueueService;
        private readonly IChannelService _channelService;

        public ArchipelagoSessionService(
            IMessageQueueService messageQueueService,
            IChannelService channelService,
			IOptions<BotSettings> settings)
        {
            _activeSessions = new Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>>();
            _messageQueueService = messageQueueService;
            _channelService = channelService;
            _settings = settings.Value;

		}

        public bool TryGetSession(ulong guildId, ulong channelId, out ArchipelagoSession? session)
        {
            session = null;
            if (_activeSessions.TryGetValue(guildId, out var guildSessions))
            {
                if (guildSessions.TryGetValue(channelId, out var activeSession))
                {
                    session = activeSession;
                    return true;
                }
            }

            return false;
        }

        public void CreateSession(CreateSessionModel model)
        {
            if (!_activeSessions.ContainsKey(model.GuildId))
            {
                _activeSessions[model.GuildId] = new Dictionary<ulong, ArchipelagoSession>();
            }

            if (_activeSessions[model.GuildId].ContainsKey(model.ChannelId))
            {
				throw new Exception("This channel is already connected to an Archipelago session.");
            }

			var session = ArchipelagoSessionFactory.CreateSession(model.IpAddress, (int)model.Port);
			LoginResult result = session.TryConnectAndLogin(model.Game, model.Name, ItemsHandlingFlags.AllItems, new Version(0, 5, 1), ["Tracker"], null, model.Password, true);

			if (result is LoginFailure failure)
			{
				var errors = string.Join("\n", failure.Errors);
                throw new Exception($"Encountered following errors when trying to log in: {errors}");
			}

			session.MessageLog.OnMessageReceived += (Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message) =>
			{
				StringBuilder FormattedMessage = new StringBuilder();
				StringBuilder RawMessage = new StringBuilder();
				//AP messages are split into parts as each word is colored depending on what it is
				//With the coloring method, we can luckily translate this to discord.
				foreach (var part in message.Parts)
				{
					FormattedMessage.Append(Utility.ColorString(part.Text, part.Color));
					RawMessage.Append(part.Text);
				}
				if (string.IsNullOrWhiteSpace(FormattedMessage.ToString())) { return; }
				Console.WriteLine($"Queueing message from AP session {model.IpAddress}:{model.Port} {model.Name} {model.Game}");
				if (!MessageHelper.CheckMessageTags(RawMessage.ToString(), _settings)) { return; }
				_messageQueueService.QueueMessage(model.Channel, FormattedMessage.ToString());
			};

			session.Socket.SocketClosed += async (reason) =>
			{
				await CleanAndCloseChannelAsync(model.GuildId, model.ChannelId);
				_messageQueueService.QueueMessage(model.Channel, $"Disconnected from Archipelago server\n{reason}");
			};

			_activeSessions[model.GuildId][model.ChannelId] = session;
		}

		private async Task CleanAndCloseChannelAsync(ulong guildId, ulong channelId)
        {
            if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session))
            {
                Console.WriteLine($"No active session found for channel {channelId} in guild {guildId}.");
                return;
            }

            var channel = _channelService.GetChannel(channelId);
            if (channel == null)
            {
                Console.WriteLine($"Channel {channelId} not found.");
                return;
            }

            Console.WriteLine($"Disconnecting Channel {channel.Id} from server {session.ConnectionInfo.Slot}");
            if (session.Socket.Connected)
            {
                try
                {
                    await session.Socket.DisconnectAsync();
                    Console.WriteLine($"Successfully disconnected channel {channelId}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting channel {channelId}: {ex.Message}");
                }
            }

            guildSessions.Remove(channelId);
            if (guildSessions.Count == 0)
            {
                _activeSessions.Remove(guildId);
                Console.WriteLine($"Removed guild {guildId} from active sessions.");
            }
        }

        public async Task RemoveSessionAsync(ulong guildId, ulong channelId)
        {
            if (_activeSessions.TryGetValue(guildId, out var guildSessions))
            {
                if (guildSessions.ContainsKey(channelId))
                {
                    guildSessions.Remove(channelId);
				}
            }

			if (_activeSessions[guildId].TryGetValue(channelId, out var session))
			{
				await session.Socket.DisconnectAsync();
				_activeSessions[guildId].Remove(channelId);
			}
        }

		public ArchipelagoSession? GetActiveSessionByChannelIdAsync(ulong guildId, ulong channelId)
		{
			if (_activeSessions.TryGetValue(guildId, out var guildSessions))
			{
				guildSessions.TryGetValue(channelId, out var session);
				return session;
			}

			return null;
		}

		public Dictionary<ulong, ArchipelagoSession> GetActiveSessionsByGuildAsync(ulong guildId)
		{
			if (_activeSessions.TryGetValue(guildId, out var guildSessions))
			{
				return guildSessions;
			}

			return new Dictionary<ulong, ArchipelagoSession>();
		}
	}
}
