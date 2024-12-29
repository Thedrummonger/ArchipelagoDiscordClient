using ArchipelagoDiscordClient.Services;
using ArchipelagoDiscordClient.Settings;
using Microsoft.Extensions.Options;

namespace ArchipelagoDiscordClient.Handlers
{
	public class MessageQueueHandler : IMessageQueueHandler
	{
		private readonly IMessageQueueService _messageQueueService;
		private readonly BotSettings _settings;

		public MessageQueueHandler(
			IMessageQueueService messageQueueService,
			IOptions<BotSettings> settings)
		{
			_messageQueueService = messageQueueService;
			_settings = settings.Value;
		}

		public async Task ProcessMessageQueueAsync()
		{
			// Discord only allows 50 API calls per second, or 1 every 20 milliseconds
			// With how fast archipelago sends data sometimes, it's really easy to hit this
			// To get around this, anytime I send a message to a discord channel I send it through this queue instead.
			// The current delay of 500 ms per action is way overkill and can probably be brought down eventually.
			while (true)
			{
				var sendQueue = _messageQueueService.GetSendQueue();

				foreach (var item in sendQueue)
				{
					if (item.Value.Count == 0) 
					{ 
						continue; 
					}

					List<string> sendBatch = new();
					var messageLength = 10; //Include the formatter (```ansi```)

					//Discord message limit is 2000, but im capping at 1500 for some padding. unfortunately I didn't write this logic well,
					//it should be checking the final message length before adding it to the final message and breaking out of the loop if
					//it *would* exceeded the limit. I'll fix it later :)
					while (item.Value.Count > 0 && messageLength < 1500)
					{
						messageLength += item.Value[0].Length + 2; // Account for new lines
						sendBatch.Add(item.Value[0]);
						item.Value.RemoveAt(0);
					}

					//We format the message in a code block with the "ansi" formatting. this makes our color codes color the discord message.
					//https://gist.github.com/kkrypt0nn/a02506f3712ff2d1c8ca7c9e0aed7c06
					var formattedMessage = $"```ansi\n{string.Join("\n\n", sendBatch)}\n```";
					Console.WriteLine($"Sending {sendBatch.Count} queued messages to channel {item.Key.Name}");

					await item.Key.SendMessageAsync(formattedMessage);
					await Task.Delay(_settings.DiscordRateLimitDelay); //Wait before processing more messages to avoid rate limit
				}

				//Wait before processing more messages to avoid rate limit
				//This await could probably be removed? or at least significantly lowered. 
				await Task.Delay(_settings.DiscordRateLimitDelay);
			}
		}
	}
}
