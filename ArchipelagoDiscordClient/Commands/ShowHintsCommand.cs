using System.Collections.Concurrent;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
	public class ShowHintsCommand : ICommand
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public string CommandName => "show_hints";

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName("show_hints")
                .WithDescription("Shows all hint for the current player")
                .Build();

        private readonly IMessageQueueService _messageQueueService;

		public ShowHintsCommand(
			Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache,
			IMessageQueueService messageQueueService)
		{
			_activeSessions = activeSessions;
			_channelCache = channelCache;
			_messageQueueService = messageQueueService;
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

			// Check if the guild and channel have an active session
			if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session))
			{
				await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
				return;
			}

			Console.WriteLine($"Showing hints for slot {session.ConnectionInfo.Slot}");

			var hints = session.DataStorage.GetHints();
			Console.WriteLine($"{hints.Length} Found");
			List<string> Messages = [];
			foreach (var hint in hints)
			{
				var FindingPlayer = session.Players.GetPlayerInfo(hint.FindingPlayer);
				var ReceivingPlayer = session.Players.GetPlayerInfo(hint.ReceivingPlayer);
				var Item = session.Items.GetItemName(hint.ItemId, ReceivingPlayer.Game);
				var Location = session.Locations.GetLocationNameFromId(hint.LocationId, FindingPlayer.Game);

				var FindingPlayerName = FindingPlayer.Name;
				var ReceivingPlayerName = ReceivingPlayer.Name;

				var FoundString = hint.Found ?
					Utility.ColorString("Found", Archipelago.MultiClient.Net.Models.Color.Green) :
					Utility.ColorString("Not Found", Archipelago.MultiClient.Net.Models.Color.Red);

				if (hint.ItemFlags.HasFlag(ItemFlags.Advancement)) { Item = Utility.ColorString(Item, Archipelago.MultiClient.Net.Models.Color.Plum); }
				else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Utility.ColorString(Item, Archipelago.MultiClient.Net.Models.Color.SlateBlue); }
				else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Utility.ColorString(Item, Archipelago.MultiClient.Net.Models.Color.Salmon); }
				else { Item = Utility.ColorString(Item, Archipelago.MultiClient.Net.Models.Color.Cyan); }

				Location = Utility.ColorString(Location, Archipelago.MultiClient.Net.Models.Color.Green);

				FindingPlayerName = FindingPlayer.Slot == session.ConnectionInfo.Slot ?
					Utility.ColorString(FindingPlayerName, Archipelago.MultiClient.Net.Models.Color.Magenta) :
					Utility.ColorString(FindingPlayerName, Archipelago.MultiClient.Net.Models.Color.Yellow);

				ReceivingPlayerName = ReceivingPlayer.Slot == session.ConnectionInfo.Slot ?
					Utility.ColorString(ReceivingPlayerName, Archipelago.MultiClient.Net.Models.Color.Magenta) :
					Utility.ColorString(FindingPlayerName, Archipelago.MultiClient.Net.Models.Color.Yellow);

				string HintLine = $"{FindingPlayerName} has {Item} at {Location} for {ReceivingPlayerName} ({FoundString})";
				Messages.Add(HintLine);
			}
			if (Messages.Count < 1)
			{
				await command.RespondAsync("No hints available for this slot.", ephemeral: true);
				return;
			}

			await command.RespondAsync($"Hints for {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}", ephemeral: false);
			foreach (var i in Messages)
			{
				_messageQueueService.QueueMessage(socketTextChannel, i);
			}
		}
	}
}
