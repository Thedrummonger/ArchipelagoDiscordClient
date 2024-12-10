using Discord;
using Discord.WebSocket;
using Archipelago.MultiClient.Net;
using Discord.Net;
using System.Text;
using Archipelago.MultiClient.Net.Packets;

namespace ArchipelagoDiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;

        // Dictionary structure: GuildID -> ChannelID -> ArchipelagoSession
        private Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions = new Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>>();
        private Dictionary<ulong, string> _originalChannelNames = new Dictionary<ulong, string>();

        private Dictionary<ISocketMessageChannel, List<string>> _sendQueue = new Dictionary<ISocketMessageChannel, List<string>>();

        static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunBotAsync();
        }

        public async Task RunBotAsync()
        {
            Task.Run(() => ProcessMessageQueueAsync());
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.All
            });

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.MessageReceived += HandleMessageReceivedAsync;

            string token;
            try
            {
                token = await File.ReadAllTextAsync("bot_token.txt");
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

            await _client.LoginAsync(TokenType.Bot, token.Trim());
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task ReadyAsync()
        {
            // Register commands globally
            var connectCommand = new SlashCommandBuilder()
                .WithName("connect")
                .WithDescription("Connect this channel to an Archipelago server")
                .AddOption("ip", ApplicationCommandOptionType.String, "Server IP", true)
                .AddOption("port", ApplicationCommandOptionType.Integer, "Server Port", true)
                .AddOption("game", ApplicationCommandOptionType.String, "Game name", true)
                .AddOption("name", ApplicationCommandOptionType.String, "Player name", true)
                .AddOption("password", ApplicationCommandOptionType.String, "Optional password", false)
                .Build();

            var disconnectCommand = new SlashCommandBuilder()
                .WithName("disconnect")
                .WithDescription("Disconnect this channel from the Archipelago server")
                .Build();

            var showSessionsCommand = new SlashCommandBuilder()
                .WithName("show_sessions")
                .WithDescription("Show all active Archipelago sessions in this server")
                .Build();

            var showChannelSessionCommand = new SlashCommandBuilder()
                .WithName("show_channel_session")
                .WithDescription("Show the active Archipelago session for this channel")
                .Build();

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(connectCommand);
                await _client.CreateGlobalApplicationCommandAsync(disconnectCommand);
                await _client.CreateGlobalApplicationCommandAsync(showSessionsCommand);
                await _client.CreateGlobalApplicationCommandAsync(showChannelSessionCommand);
            }
            catch (HttpException ex)
            {
                Console.WriteLine($"Error registering commands: {ex.Message}");
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.CommandName)
            {
                case "connect":
                    await HandleConnectCommand(command);
                    break;

                case "disconnect":
                    await HandleDisconnectCommand(command);
                    break;

                case "show_sessions":
                    await HandleShowSessionsCommand(command);
                    break;

                case "show_channel_session":
                    await HandleShowChannelSessionCommand(command);
                    break;
            }
        }

        private async Task HandleConnectCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;
            var channelId = command.Channel.Id;
            string channelName = command.Channel?.Name ?? channelId.ToString();

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

            // Extract parameters
            var ip = command.Data.Options.FirstOrDefault(option => option.Name == "ip")?.Value as string;
            var port = (long?)command.Data.Options.FirstOrDefault(option => option.Name == "port")?.Value;
            var game = command.Data.Options.FirstOrDefault(option => option.Name == "game")?.Value as string;
            var name = command.Data.Options.FirstOrDefault(option => option.Name == "name")?.Value as string;
            var password = command.Data.Options.FirstOrDefault(option => option.Name == "password")?.Value as string;

            Console.WriteLine($"Connecting {channelId} to {ip}:{port} as {name} playing {game}");
            await command.RespondAsync($"Connecting {channelName} to {ip}:{port} as {name} playing {game}...");
            //await command.DeferAsync();

            // Create a new session
            try
            {
                var session = ArchipelagoSessionFactory.CreateSession(ip, (int)port);
                Console.WriteLine($"Trying to connect");
                LoginResult result = session.TryConnectAndLogin(game, name, Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems, password: password);

                if (result is LoginFailure failure)
                {
                    var errors = string.Join("\n", failure.Errors);
                    await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect to Archipelago server at {ip}:{port} as {name}.\n{errors}");
                    return;
                }

                Console.WriteLine($"Connected");
                // Register the OnMessageReceived event handler
                session.MessageLog.OnMessageReceived += async (Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message) =>
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (var part in message.Parts) { stringBuilder.Append(part.Text); }
                    if (_client.GetChannel(channelId) is not ISocketMessageChannel channel) { return; }
                    if (string.IsNullOrWhiteSpace(stringBuilder.ToString())) { return; }
                    QueueMessage(channel, stringBuilder.ToString());
                    //await channel.SendMessageAsync(stringBuilder.ToString());
                };
                session.Socket.SocketClosed += async (reason) =>
                {
                    await CleanAndCloseChannel(guildId, channelId);
                };

                // Store the session
                _activeSessions[guildId][channelId] = session;

                if (command.Channel is SocketTextChannel textChannel)
                {
                    _originalChannelNames[channelId] = textChannel.Name;
                    await textChannel.ModifyAsync(prop => prop.Name = $"{name}_{game}_{ip.Replace(".", "-")}-{port}");
                }

                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully connected channel {channelName} to Archipelago server at {ip}:{port} as {name}.");
            }
            catch (Exception ex)
            {
                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect: {ex.Message}");
            }
        }

        private void QueueMessage(ISocketMessageChannel channel, string Message)
        {
            if (!_sendQueue.ContainsKey(channel)) { _sendQueue[channel] = []; }
            _sendQueue[channel].Add(Message);
        }

        private async Task HandleDisconnectCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;
            var channelId = command.Channel.Id;

            // Check if the guild and channel have an active session
            if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session))
            {
                await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                return;
            }

            await CleanAndCloseChannel(guildId, channelId);

            await command.RespondAsync("Successfully disconnected from the Archipelago server.");
        }

        private async Task CleanAndCloseChannel(ulong guildId, ulong channelId)
        {
            if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session)) { return; }
            if (session.Socket.Connected) { await session.Socket.DisconnectAsync(); }
            guildSessions.Remove(channelId);
            if (guildSessions.Count == 0)
            {
                _activeSessions.Remove(guildId);
            }
            var channel = _client.GetChannel(channelId);
            if (channel is SocketTextChannel textChannel && _originalChannelNames.TryGetValue(channelId, out var originalName))
            {
                await textChannel.ModifyAsync(prop => prop.Name = originalName);
                _originalChannelNames.Remove(channelId);
            }
        }

        private async Task HandleShowSessionsCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;

            // Check if the guild has active sessions
            if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || guildSessions.Count == 0)
            {
                await command.RespondAsync("No active Archipelago sessions in this guild.", ephemeral: true);
                return;
            }

            // Build the response
            var response = "Active Archipelago Sessions:\n";
            foreach (var (channelId, session) in guildSessions)
            {
                var channel = _client.GetChannel(channelId) as SocketTextChannel;
                if (channel == null) continue;

                response += $"- **Channel**: {channel.Name}\n" +
                            $"  **Server**: {session.Socket.Uri}\n" +
                            $"  **Player**: {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}({session.ConnectionInfo.Slot})\n";
            }

            await command.RespondAsync(response, ephemeral: true);
        }

        private async Task HandleShowChannelSessionCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;
            var channelId = command.Channel.Id;

            // Check if the channel has an active session
            if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session))
            {
                await command.RespondAsync("No active Archipelago session in this channel.", ephemeral: true);
                return;
            }

            // Build the response
            var response = $"**Active Archipelago Session**\n" +
                           $"  **Server**: {session.Socket.Uri}\n" +
                           $"  **Player**: {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}({session.ConnectionInfo.Slot})\n";

            await command.RespondAsync(response, ephemeral: true);
        }

        private async Task HandleMessageReceivedAsync(SocketMessage message)
        {
            // Ignore messages from bots
            if (message.Author.IsBot) return;

            // Check if the message was sent in a guild text channel
            if (message.Channel is not SocketTextChannel textChannel) return;

            var guildId = textChannel.Guild.Id;
            var channelId = textChannel.Id;

            // Check if the channel exists in our dictionary
            if (_activeSessions.TryGetValue(guildId, out var guildSessions) && guildSessions.TryGetValue(channelId, out var session))
            {
                if (string.IsNullOrWhiteSpace(message.Content)) { return; }
                try
                {
                    // Send the message to the Archipelago server
                    session.Socket.SendPacket(new SayPacket() { Text = message.Content } );
                    Console.WriteLine($"Message sent to Archipelago from {message.Author.Username}: {message.Content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
                    await textChannel.SendMessageAsync("Error: Unable to send message to the Archipelago server.");
                }
            }
        }

        private Task LogAsync(Discord.LogMessage logMessage)
        {
            Console.WriteLine(logMessage.ToString());
            return Task.CompletedTask;
        }
        private async Task ProcessMessageQueueAsync()
        {
            while (true)
            {
                foreach (var i in _sendQueue)
                {
                    if (i.Value.Count > 0)
                    {
                        await i.Key.SendMessageAsync(string.Join('\n', i.Value));
                        i.Value.Clear();
                    }
                }
                // No messages to process; wait briefly
                await Task.Delay(500);
            }
        }
    }
}