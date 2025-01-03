﻿using System.Collections.Concurrent;
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
			services.AddSingleton(new ConcurrentDictionary<ulong, SocketTextChannel>());

			RegisterCommands(services);
			RegisterServices(services);
			RegisterHandlers(services);
		}

		private static void RegisterCommands(IServiceCollection services)
		{
			services.AddSingleton<ICommand, ConnectCommand>();
			services.AddSingleton<ICommand, DisconnectCommand>();
			services.AddSingleton<ICommand, ShowSessionsCommand>();
			services.AddSingleton<ICommand, ShowChannelSessionCommand>();
			services.AddSingleton<ICommand, ShowHintsCommand>();
      services.AddSingleton<ICommand, IgnoreClientsCommand>();
      services.AddSingleton<ICommand, AddIgnoreTypesCommand>();
      services.AddSingleton<ICommand, DelIgnoreTypesCommand>();
      services.AddSingleton<ICommand, ListIgnoreTypesCommand>();
			services.AddSingleton<ICommand, AssignUserToPlayerCommand>();
		}

		private static void RegisterServices(IServiceCollection services)
		{
			services.AddSingleton<ICommandService, CommandService>();
			services.AddSingleton<IDiscordCommandRegistrationService, DiscordCommandRegistrationService>();
			services.AddSingleton<IChannelService, ChannelService>();
			services.AddSingleton<IMessageQueueService, MessageQueueService>();
			services.AddSingleton<IFileService, JsonFileService>();
			services.AddSingleton<IArchipelagoSessionService, ArchipelagoSessionService>();
		}

		private static void RegisterHandlers(IServiceCollection services)
		{
			services.AddSingleton<IDiscordEventHandler, DiscordEventHandler>();
			services.AddSingleton<IDiscordMessageHandler, DiscordMessageHandler>();
			services.AddSingleton<IMessageQueueHandler, MessageQueueHandler>();
		}
	}
}
