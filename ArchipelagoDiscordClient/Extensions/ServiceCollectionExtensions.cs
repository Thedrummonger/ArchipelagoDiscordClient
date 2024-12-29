using System.Collections.Concurrent;
using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Commands;
using ArchipelagoDiscordClient.Handlers;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ArchipelagoDiscordClient.Extensions
{
    public static class ServiceCollectionExtensions
	{
		public static void ConfigureServices(this IServiceCollection services)
		{
			// Register discord client
			var config = new DiscordSocketConfig()
			{
				LogLevel = LogSeverity.Info,
				GatewayIntents = GatewayIntents.All // I'm to lazy to figure out exactly what intents are needed, gimme all
			};

			services.AddSingleton(config);
			services.AddSingleton<DiscordSocketClient>();

			// Register shared state
			services.AddSingleton(new Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>>());
			services.AddSingleton(new ConcurrentDictionary<ulong, SocketTextChannel>());

			// Register services
			services.AddSingleton<ICommand, ConnectCommand>();
			services.AddSingleton<ICommand, DisconnectCommand>();
			services.AddSingleton<ICommand, ShowSessionsCommand>();
			services.AddSingleton<ICommand, ShowChannelSessionCommand>();
			services.AddSingleton<ICommand, ShowHintsCommand>();
            services.AddSingleton<ICommand, IgnoreClientsCommand>();
            services.AddSingleton<ICommandService, CommandService>();
			services.AddSingleton<IDiscordCommandRegistrationService, DiscordCommandRegistrationService>();
			services.AddSingleton<IChannelService, ChannelService>();
			services.AddSingleton<IMessageQueueService, MessageQueueService>();

			// Register handlers
			services.AddSingleton<IDiscordEventHandler, DiscordEventHandler>();
			services.AddSingleton<IDiscordMessageHandler, DiscordMessageHandler>();
			services.AddSingleton<IMessageQueueHandler, MessageQueueHandler>();
		}
	}
}
