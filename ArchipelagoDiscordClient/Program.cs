using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ArchipelagoDiscordClient.Handlers;
using ArchipelagoDiscordClient.Settings;
using Microsoft.Extensions.Configuration;
using ArchipelagoDiscordClient.Extensions;
using Microsoft.Extensions.Options;
using TDMUtils;
using ArchipelagoDiscordClient.Constants;

namespace ArchipelagoDiscordClient
{
	class Program
    {
        static async Task Main(string[] args)
        {
			var program = new Program();
            await program.RunBotAsync();
        }

        public async Task RunBotAsync()
        {
            if (!Path.Exists(FilePaths.BaseFilePath)) 
			{
				Directory.CreateDirectory(FilePaths.BaseFilePath); 
			}
            if (!File.Exists(FilePaths.ConfigFileFullPath))
            {
                File.WriteAllText(FilePaths.ConfigFileFullPath, new BotSettings().ToFormattedJson());
            }

            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(FilePaths.ConfigFileFullPath, optional: false, reloadOnChange: true)
                .Build();

            serviceCollection.Configure<BotSettings>(configuration);

            // Call the method to register services
            serviceCollection.ConfigureServices();
			var serviceProvider = serviceCollection.BuildServiceProvider();
			var discordEventHandler = serviceProvider.GetRequiredService<IDiscordEventHandler>();
			discordEventHandler.SubscribeToDiscordEvents();

			var botSettings = serviceProvider.GetRequiredService<IOptions<BotSettings>>().Value;

            if (string.IsNullOrEmpty(botSettings.BotToken))
			{
				throw new Exception($"Please enter you bot token in {FilePaths.ConfigFileFullPath}");
            }

            var discordClient = serviceProvider.GetRequiredService<DiscordSocketClient>();

			await discordClient.LoginAsync(TokenType.Bot, botSettings.BotToken);
            await discordClient.StartAsync();

			//Run a background task to constantly send messages in the send queue
			var messageQueueHandler = serviceProvider.GetRequiredService<IMessageQueueHandler>();
			_ = Task.Run(messageQueueHandler.ProcessMessageQueueAsync);

			//TODO, maybe just replace this with a loop to handle console commands on the server it's self
			await Task.Delay(-1);
        }
    }
}