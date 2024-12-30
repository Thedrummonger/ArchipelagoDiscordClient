using System.Collections.Concurrent;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Helpers;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
    public class ShowHintsCommand : ICommand
	{
		private readonly IArchipelagoSessionService _sessionService;
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;

		public string CommandName => CommandTypes.ShowHintsCommand;

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Shows all hint for the current player")
                .Build();

        private readonly IMessageQueueService _messageQueueService;

		public ShowHintsCommand(
			IArchipelagoSessionService sessionService,
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache,
			IMessageQueueService messageQueueService)
		{
			_sessionService = sessionService;
			_channelCache = channelCache;
			_messageQueueService = messageQueueService;
		}

		public async Task ExecuteAsync(SocketSlashCommand command)
		{
            var commandData = command.GetCommandData();
			if (commandData.socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}
			_channelCache.TryAdd(commandData.channelId, commandData.socketTextChannel);

			var session = _sessionService.GetActiveSessionByChannelIdAsync(commandData.guildId, commandData.channelId);
			if (session is null)
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
                    ColorHelper.SetColor("Found", Archipelago.MultiClient.Net.Models.Color.Green) :
                    ColorHelper.SetColor("Not Found", Archipelago.MultiClient.Net.Models.Color.Red);

				if (hint.ItemFlags.HasFlag(ItemFlags.Advancement)) { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.Plum); }
				else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.SlateBlue); }
				else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.Salmon); }
				else { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.Cyan); }

				Location = Location.SetColor(Archipelago.MultiClient.Net.Models.Color.Green);

				FindingPlayerName = FindingPlayer.Slot == session.ConnectionInfo.Slot ?
                    FindingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Magenta) :
                    FindingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Yellow);

				ReceivingPlayerName = ReceivingPlayer.Slot == session.ConnectionInfo.Slot ?
                    ReceivingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Magenta) :
                    ReceivingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Yellow);

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
				_messageQueueService.QueueMessage(commandData.socketTextChannel, i);
			}
		}
	}
}
