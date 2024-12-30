using Discord.WebSocket;
using System.Collections.Concurrent;
using Discord;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Handlers;
using ArchipelagoDiscordClient.Models;
using ArchipelagoDiscordClient.Helpers;

namespace ArchipelagoDiscordClient.Commands
{
	public class ConnectCommand : ICommand
	{
		private readonly ConcurrentDictionary<ulong, SocketTextChannel> _channelCache;
		private readonly IArchipelagoSessionService _sessionService;

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
			ConcurrentDictionary<ulong, SocketTextChannel> channelCache,
			IArchipelagoSessionService sessionService)
		{
			_channelCache = channelCache;
			_sessionService = sessionService;
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
			var model = new CreateSessionModel
			{
				GuildId = commandData.guildId,
				ChannelId = commandData.channelId,
				IpAddress = (string)commandData.GetArg("ip")!.Value,
				Port = (long)commandData.GetArg("port")!.Value!,
				Game = (string)commandData.GetArg("game")!.Value,
				Name = (string)commandData.GetArg("name")!.Value,
				Password = (string?)commandData.GetArg("password")?.Value,
				Channel = commandData.socketTextChannel
			};

			Console.WriteLine($"Connecting {commandData.channelId} to {model.IpAddress}:{model.Port} as {model.Name} playing {model.Game}");
			await command.RespondAsync($"Connecting {commandData.channelId} to {model.IpAddress}:{model.Port} as {model.Name} playing {model.Game}...");

			try
			{
				_sessionService.CreateSession(model);
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully connected channel {commandData.channelName} to Archipelago server at {model.IpAddress}:{model.Port} as {model.Name}.");
			}
			catch (Exception ex)
			{
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect: {ex.Message}");
			}
		}
	}
}
