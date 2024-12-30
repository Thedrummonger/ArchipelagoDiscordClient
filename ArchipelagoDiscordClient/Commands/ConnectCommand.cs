using Discord.WebSocket;
using System.Collections.Concurrent;
using Discord;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Handlers;
using ArchipelagoDiscordClient.Models;

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
			Utility.GetCommandData(command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel);
			if (socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}

			_channelCache.TryAdd(channelId, socketTextChannel);
			var model = new CreateSessionModel
			{
				GuildId = guildId,
				ChannelId = channelId,
				IpAddress = (string)command.Data.Options.FirstOrDefault(option => option.Name == "ip")!.Value,
				Port = (long)command.Data.Options.FirstOrDefault(option => option.Name == "port")!.Value!,
				Game = (string)command.Data.Options.FirstOrDefault(option => option.Name == "game")!.Value,
				Name = (string)command.Data.Options.FirstOrDefault(option => option.Name == "name")!.Value,
				Password = (string?)command.Data.Options.FirstOrDefault(option => option.Name == "password")?.Value,
				Channel = socketTextChannel
			};

			Console.WriteLine($"Connecting {channelId} to {model.IpAddress}:{model.Port} as {model.Name} playing {model.Game}");
			await command.RespondAsync($"Connecting {channelId} to {model.IpAddress}:{model.Port} as {model.Name} playing {model.Game}...");

			try
			{
				_sessionService.CreateSession(model);
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully connected channel {channelName} to Archipelago server at {model.IpAddress}:{model.Port} as {model.Name}.");
			}
			catch (Exception ex)
			{
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect: {ex.Message}");
			}
		}
	}
}
