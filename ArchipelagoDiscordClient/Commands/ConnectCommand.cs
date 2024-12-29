using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Text;
using ArchipelagoDiscordClient.Services;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ArchipelagoDiscordClient.Settings;
using Discord;
using ArchipelagoDiscordClient.Constants;

namespace ArchipelagoDiscordClient.Commands
{
	public class ConnectCommand : ICommand
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

        private readonly IChannelService _channelService;
		private readonly IMessageQueueService _messageQueueService;
		private readonly BotSettings _settings;

		public string CommandName => CommandTypes.ConnectCommand;
        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Connect this channel to an Archipelago server")
                .AddOption("ip", ApplicationCommandOptionType.String, "Server IP", true)
                .AddOption("port", ApplicationCommandOptionType.Integer, "Server Port", true)
                .AddOption("game", ApplicationCommandOptionType.String, "Game name", true)
                .AddOption("name", ApplicationCommandOptionType.String, "Player name", true)
                .AddOption("password", ApplicationCommandOptionType.String, "Optional password", false)
                .Build();

        public ConnectCommand(
			Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache,
			IChannelService channelService,
			IMessageQueueService messageQueueService,
			IOptions<BotSettings> settings)
		{
			_activeSessions = activeSessions;
			_channelCache = channelCache;
			_channelService = channelService;
			_messageQueueService = messageQueueService;
			_settings = settings.Value;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
			Utility.GetCommandData(command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel);
			if (socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}
			_channelCache.TryAdd(channelId, socketTextChannel);

			// Ensure the guild dictionary exists
			if (!_activeSessions.ContainsKey(guildId))
			{
				_activeSessions[guildId] = [];
			}

			// Ensure the channel is not already connected
			if (_activeSessions[guildId].ContainsKey(channelId))
			{
				await command.RespondAsync("This channel is already connected to an Archipelago session.", ephemeral: true);
				return;
			}

			// Extract parameters (surely there is a better way to do this? but this is how Discord.net documentation said to do it)
			var ip = command.Data.Options.FirstOrDefault(option => option.Name == "ip")?.Value as string;
			var port = (long?)command.Data.Options.FirstOrDefault(option => option.Name == "port")?.Value;
			var game = command.Data.Options.FirstOrDefault(option => option.Name == "game")?.Value as string;
			var name = command.Data.Options.FirstOrDefault(option => option.Name == "name")?.Value as string;
			var password = command.Data.Options.FirstOrDefault(option => option.Name == "password")?.Value as string;

			Console.WriteLine($"Connecting {channelId} to {ip}:{port} as {name} playing {game}");
			await command.RespondAsync($"Connecting {channelName} to {ip}:{port} as {name} playing {game}...");

			// Create a new session
			try
			{
				var session = ArchipelagoSessionFactory.CreateSession(ip, (int)port);
				Console.WriteLine($"Trying to connect");
				//Probably doesn't need ItemsHandlingFlags.AllItems
				LoginResult result = session.TryConnectAndLogin(game, name, ItemsHandlingFlags.AllItems, new Version(0, 5, 1), ["Tracker"], null, password, true);

				if (result is LoginFailure failure)
				{
					var errors = string.Join("\n", failure.Errors);
					await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect to Archipelago server at {ip}:{port} as {name}.\n{errors}");
					return;
				}

				Console.WriteLine($"Connected");
				var originalChannelNames = new Dictionary<ulong, string>();
				// Register the OnMessageReceived event handler
				session.MessageLog.OnMessageReceived += (Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message) =>
				{
					StringBuilder FormattedMessage = new StringBuilder();
					StringBuilder RawMessage = new StringBuilder();
					//AP messages are split into parts as each word is colored depending on what it is
					//With the coloring method, we can luckily translate this to discord.
					foreach (var part in message.Parts)
					{
						FormattedMessage.Append(Utility.ColorString(part.Text, part.Color));
					}
					if (string.IsNullOrWhiteSpace(FormattedMessage.ToString())) { return; }
					Console.WriteLine($"Queueing message from AP session {ip}:{port} {name} {game}");
					if (!CheckMessageTags(RawMessage.ToString())) { return; }
					_messageQueueService.QueueMessage(socketTextChannel, FormattedMessage.ToString());
				};
				session.Socket.SocketClosed += async (reason) =>
				{
					await CleanAndCloseChannel(guildId, channelId, originalChannelNames);
					_messageQueueService.QueueMessage(socketTextChannel, $"Disconnected from Archipelago server\n{reason}");
				};

				// Store the session
				_activeSessions[guildId][channelId] = session;

				//TODO revisit this, it works but for some reason it keeps triggering a rate limit from discord.
				if (command.Channel is SocketTextChannel textChannel)
				{
					//_originalChannelNames[channelId] = textChannel.Name;
					//await textChannel.ModifyAsync(prop => prop.Name = $"{name}_{game}_{ip.Replace(".", "-")}-{port}");
				}

				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully connected channel {channelName} to Archipelago server at {ip}:{port} as {name}.");
			}
			catch (Exception ex)
			{
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect: {ex.Message}");
			}
		}

		private async Task CleanAndCloseChannel(ulong guildId, ulong channelId, Dictionary<ulong, string> originalChannelNames)
		{
			if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session)) { return; }
			var channel = _channelService.GetChannel(channelId);
			if (channel == null) { return; }
			Console.WriteLine($"Disconnecting Channel {channel.Id} from server {session.ConnectionInfo.Slot}");
			if (session.Socket.Connected) { await session.Socket.DisconnectAsync(); }
			guildSessions.Remove(channelId);
			if (guildSessions.Count == 0)
			{
				_activeSessions.Remove(guildId);
			}

			//TODO revisit this, it works but for some reason it keeps triggering a rate limit from discord.
			if (channel is SocketTextChannel textChannel && originalChannelNames.TryGetValue(channelId, out var originalName))
			{
				//await textChannel.ModifyAsync(prop => prop.Name = originalName);
				//_originalChannelNames.Remove(channelId);
			}
		}

		private bool CheckMessageTags(string input)
		{
			bool IsClient = Utility.IsClientNotification(input, out Version version);
			if (!IsClient) { return true; }
			Console.WriteLine($"Client Connecting V{version}");
			if (IsClient && _settings.IgnoreAllClientMessages) { return false; }

			/* Not sure if I'll ever use this, but I don't want to write the regex again
            int Team = -1;
            string TeamPattern = @"\(Team #(\d+)\)";
            Match TeamMatch = Regex.Match(input, TeamPattern);
            if (TeamMatch.Success && int.TryParse(TeamMatch.Groups[1].Value, out int ParsedTeam))
                Team = ParsedTeam;
            */

			string TagPattern = @"\[(.*?)\]"; //Tags are defined in brackets
			MatchCollection TagMatches = Regex.Matches(input, TagPattern);
			HashSet<string> tags = [];
			HashSet<string> IgnoredTags = [];
			if (TagMatches.Count > 0)
			{
				string lastMatch = TagMatches[^1].Groups[1].Value; //Tags are always at the end of a message
				string[] parts = lastMatch.Split(',');             //I don't think any brackets would ever appear other than the tags
				foreach (string part in parts)                     //but just pick the last instance of brackets to be safe
				{
					string PartTrimmed = part.Trim();
					if (!PartTrimmed.StartsWith("'") || !PartTrimmed.EndsWith("'")) { continue; } //Probably more unnecessary checking
					string Tag = PartTrimmed[1..^1].ToLower();                                    //But tags are always in single quotes
					tags.Add(Tag);
					if (_settings.IgnoreTags.Contains(Tag))
						IgnoredTags.Add(Tag);
				}
			}
			Console.WriteLine($"Client Tags [{String.Join(",", tags)}]");
			if (IgnoredTags.Count > 0)
			{
				Console.WriteLine($"Client was Ignored Type [{String.Join(",", IgnoredTags)}]");
				return false;
			}
			return true;
		}
	}
}
