using System.Collections.Concurrent;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDiscordClient.Services;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Handlers
{
	public class DiscordMessageHandler : IDiscordMessageHandler
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly IMessageQueueService _messageQueueService;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public DiscordMessageHandler(
			Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions,
			IMessageQueueService messageQueueService,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache)
		{
			_activeSessions = activeSessions;
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

			// Check if the channel exists in our dictionary
			if (_activeSessions.TryGetValue(guildId, out var guildSessions) && guildSessions.TryGetValue(channelId, out var session))
			{
				if (string.IsNullOrWhiteSpace(message.Content)) { return; }
				string Message = $"[Discord: {message.Author.Username}] {message.Content}";
				try
				{
					// Send the message to the Archipelago server
					await session.Socket.SendPacketAsync(new SayPacket() { Text = Message });
					Console.WriteLine($"Message sent to Archipelago from {message.Author.Username} in {message.Channel.Name}: {message.Content}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
					_messageQueueService.QueueMessage(textChannel, $"Error: Unable to send message to the Archipelago server.\n{ex.Message}");
					//await textChannel.SendMessageAsync("Error: Unable to send message to the Archipelago server.");
				}
			}
		}
	}
}
