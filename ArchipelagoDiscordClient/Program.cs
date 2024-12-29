using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ArchipelagoDiscordClient.Handlers;
using ArchipelagoDiscordClient.Settings;
using Microsoft.Extensions.Configuration;
using ArchipelagoDiscordClient.Extensions;

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
			var serviceCollection = new ServiceCollection();
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

			// Register configuration section (e.g., BotSettings)
			serviceCollection.Configure<BotSettings>(configuration.GetSection("BotSettings"));

			// Call the method to register services
			serviceCollection.ConfigureServices();
			var serviceProvider = serviceCollection.BuildServiceProvider();
			var discordEventHandler = serviceProvider.GetRequiredService<IDiscordEventHandler>();
			discordEventHandler.SubscribeToDiscordEvents();

			if (!File.Exists("bot_token.txt"))
            {
                Console.WriteLine("Please enter Discord Bot Token");
                var UserToken = Console.ReadLine();
                File.WriteAllText("bot_token.txt", UserToken);
            }

            string token;
            try
            {
                token = File.ReadAllText("bot_token.txt");
                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("The bot token file is empty.");
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error: bot_token.txt not found. Please create the file and add your bot token.");
                return;
			}

			var discordClient = serviceProvider.GetRequiredService<DiscordSocketClient>();

			await discordClient.LoginAsync(TokenType.Bot, token.Trim());
            await discordClient.StartAsync();

			//Run a background task to constantly send messages in the send queue
			var messageQueueHandler = serviceProvider.GetRequiredService<IMessageQueueHandler>();
			_ = Task.Run(messageQueueHandler.ProcessMessageQueueAsync);

			//TODO, maybe just replace this with a loop to handle console commands on the server it's self
			await Task.Delay(-1);
        }
    }
}