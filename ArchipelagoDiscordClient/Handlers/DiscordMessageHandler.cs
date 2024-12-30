using System.Collections.Concurrent;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDiscordClient.Services;
using Discord.WebSocket;
using TDMUtils;

namespace ArchipelagoDiscordClient.Handlers
{
	public class DiscordMessageHandler : IDiscordMessageHandler
	{
		private readonly IArchipelagoSessionService _sessionService;
		private readonly IMessageQueueService _messageQueueService;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public DiscordMessageHandler(
			IArchipelagoSessionService sessionService,
			IMessageQueueService messageQueueService,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache)
		{
            _sessionService = sessionService;
			_messageQueueService = messageQueueService;
			_channelCache = channelCache;
		}

		// TODO: Due to the way Archipelago handles "chat" messages, any message sent to AP 
		// is broadcast to all clients, including the one that originally sent it. 
		// This results in messages being duplicated in the Discord chat.
		// Ideally, I want to avoid posting a message to Discord if it originated 
		// from the same channel, but I can't think of a good way to track that.
		public async Task HandleMessageReceivedAsync(SocketMessage message)
		{
			// Ignore messages from bots
			if (message.Author.IsBot) return;

			// Check if the message was sent in a guild text channel
			if (message.Channel is not SocketTextChannel textChannel) { return; }
			_channelCache.TryAdd(message.Channel.Id, textChannel);

			var guildId = textChannel.Guild.Id;
			var channelId = textChannel.Id;

			var activeSession = _sessionService.GetActiveSessionByChannelIdAsync(guildId, channelId);
			if (activeSession is null) { return; }
            if (message.Content.IsNullOrWhiteSpace()) { return; }
            string discordMessage = $"[Discord: {message.Author.Username}] {message.Content}";
            try
            {
                // Send the message to the Archipelago server
                await activeSession.Socket.SendPacketAsync(new SayPacket() { Text = discordMessage });
                Console.WriteLine($"Message sent to Archipelago from {message.Author.Username} in {message.Channel.Name}: {message.Content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
                _messageQueueService.QueueMessage(textChannel, $"Error: Unable to send message to the Archipelago server.\n{ex.Message}");
            }
		}
	}
}
