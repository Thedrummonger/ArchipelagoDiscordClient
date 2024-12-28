using Discord;
using Discord.WebSocket;
using Archipelago.MultiClient.Net;
using Discord.Net;
using System.Text;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Enums;
using System.Text.RegularExpressions;

namespace ArchipelagoDiscordClient
{
    class Program
    {
        private DiscordSocketClient _client;

        // Dictionary structure: GuildID -> ChannelID -> ArchipelagoSession
        private Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions = new Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>>();
        private Dictionary<ulong, string> _originalChannelNames = new Dictionary<ulong, string>();

        private Dictionary<SocketTextChannel, List<string>> _sendQueue = new Dictionary<SocketTextChannel, List<string>>();

        private Dictionary<ulong, SocketTextChannel?> ChannelCache = new Dictionary<ulong, SocketTextChannel?>();

        private HashSet<string> IgnoreTags = ["tracker"];
        private bool IgnoreAllClients = false;

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

            if (!File.Exists("bot_token.txt"))
            {
                Console.WriteLine("Please enter Discord Bot Token");
                var UserToken = Console.ReadLine();
                File.WriteAllText("bot_token.txt", UserToken);
            }

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

            var showHintsCommand = new SlashCommandBuilder()
                .WithName("show_hints")
                .WithDescription("Shows all hint for the current player")
                .Build();

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(connectCommand);
                await _client.CreateGlobalApplicationCommandAsync(disconnectCommand);
                await _client.CreateGlobalApplicationCommandAsync(showSessionsCommand);
                await _client.CreateGlobalApplicationCommandAsync(showChannelSessionCommand);
                await _client.CreateGlobalApplicationCommandAsync(showHintsCommand);
            }
            catch (HttpException ex)
            {
                Console.WriteLine($"Error registering commands: {ex.Message}");
            }
        }

        public SocketTextChannel? GetChannel(ulong id)
        {
            if (!ChannelCache.TryGetValue(id, out SocketTextChannel? channel))
            {
                SocketTextChannel? Channel = null;
                if (_client.GetChannel(id) is SocketTextChannel TC) { Channel = TC; }
                ChannelCache.Add(id, Channel);
            }
            return channel;
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

                case "show_hints":
                    await HandleShowHintsCommand(command);
                    break;
            }
        }

        private async Task HandleShowHintsCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;
            var channelId = command.Channel.Id;
            if (command.Channel is not SocketTextChannel socketTextChannel)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true); 
                return; 
            }
            ChannelCache.TryAdd(channelId, socketTextChannel);

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

                if (FindingPlayer.Slot == session.ConnectionInfo.Slot) 
                { 
                    FindingPlayerName = Utility.ColorString(FindingPlayerName, Archipelago.MultiClient.Net.Models.Color.Magenta); 
                }
                else
                {
                    FindingPlayerName = Utility.ColorString(FindingPlayerName, Archipelago.MultiClient.Net.Models.Color.Yellow);
                }

                if (ReceivingPlayer.Slot == session.ConnectionInfo.Slot)
                {
                    ReceivingPlayerName = Utility.ColorString(ReceivingPlayerName, Archipelago.MultiClient.Net.Models.Color.Magenta);
                }
                else
                {
                    ReceivingPlayerName = Utility.ColorString(FindingPlayerName, Archipelago.MultiClient.Net.Models.Color.Yellow);
                }

                string HintLine = $"{FindingPlayerName} has {Item} at {Location} for {ReceivingPlayerName} ({FoundString})";
                Messages.Add(HintLine);
            }
            if (Messages.Count < 1) 
            {
                await command.RespondAsync("No hints available for this slot.", ephemeral: true);
                return; 
            }
            //var Message = $"```ansi\n{string.Join('\n', Messages)}\n```";
            await command.RespondAsync($"Hints for {session.Players.GetPlayerName(session.ConnectionInfo.Slot)}", ephemeral: false);
            foreach (var i in Messages) 
            {
                QueueMessage(socketTextChannel, i);
            }
            //await channel.SendMessageAsync(Message);
        }

        private async Task HandleConnectCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;
            var channelId = command.Channel.Id;
            string channelName = command.Channel?.Name ?? channelId.ToString();
            if (command.Channel is not SocketTextChannel socketTextChannel)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                return;
            }
            ChannelCache.TryAdd(channelId, socketTextChannel);

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
                LoginResult result = session.TryConnectAndLogin(game, name, ItemsHandlingFlags.AllItems, new Version(0, 5, 1), ["Tracker"], null, password, true);

                if (result is LoginFailure failure)
                {
                    var errors = string.Join("\n", failure.Errors);
                    await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect to Archipelago server at {ip}:{port} as {name}.\n{errors}");
                    return;
                }

                Console.WriteLine($"Connected");
                // Register the OnMessageReceived event handler
                session.MessageLog.OnMessageReceived += (Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message) =>
                {
                    StringBuilder FormattedMessage = new StringBuilder();
                    StringBuilder RawMessage = new StringBuilder();
                    foreach (var part in message.Parts) 
                    { 
                        FormattedMessage.Append(Utility.ColorString(part.Text, part.Color));
                        FormattedMessage.Append(part.Text);
                    }
                    if (GetChannel(channelId) is not SocketTextChannel channel) { return; }
                    if (string.IsNullOrWhiteSpace(FormattedMessage.ToString())) { return; }
                    Console.WriteLine($"Queueing message from AP session {ip}:{port} {name} {game}");
                    if (!CheckMessageTags(RawMessage.ToString())) { return; }
                    QueueMessage(channel, FormattedMessage.ToString());
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

        private bool CheckMessageTags(string input)
        {
            bool IsClient = Utility.IsClientNotification(input, out Version version);
            if (!IsClient) { return true; }
            Console.WriteLine($"Client Connecting V{version}");
            if (IsClient && IgnoreAllClients) { return false; }

            string TagPattern = @"\[(.*?)\]"; //Tags are defined in brackets
            MatchCollection matches = Regex.Matches(input, TagPattern);
            HashSet<string> tags = [];
            HashSet<string> IgnoredTags = [];
            if (matches.Count > 0)
            {
                string lastMatch = matches[^1].Groups[1].Value; //Tags are always at the end of a message
                string[] parts = lastMatch.Split(',');          //I don't think any brackets would ever appear other than the tags
                foreach (string part in parts)                  //but just pick the last instance of brackets to be safe
                {
                    string PartTrimmed = part.Trim();
                    if (!PartTrimmed.StartsWith("'") || !PartTrimmed.EndsWith("'")) { continue; } //Probably more unnecessary checking
                    string Tag = PartTrimmed[1..^1].ToLower();                                    //But tags are always in single quotes
                    tags.Add(Tag);
                    if (IgnoreTags.Contains(Tag))
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

        private void QueueMessage(SocketTextChannel channel, string Message)
        {
            if (!_sendQueue.ContainsKey(channel)) { _sendQueue[channel] = []; }
            _sendQueue[channel].Add(Message);
        }

        private async Task HandleDisconnectCommand(SocketSlashCommand command)
        {
            var guildId = command.GuildId ?? 0;
            var channelId = command.Channel.Id;
            if (command.Channel is not SocketTextChannel socketTextChannel)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                return;
            }
            ChannelCache.TryAdd(channelId, socketTextChannel);

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
            var channel = GetChannel(channelId);
            if (channel == null) { return; }
            Console.WriteLine($"Disconnecting Channel {channel.Id} from server {session.ConnectionInfo.Slot}");
            if (session.Socket.Connected) { await session.Socket.DisconnectAsync(); }
            guildSessions.Remove(channelId);
            if (guildSessions.Count == 0)
            {
                _activeSessions.Remove(guildId);
            }
            if (channel is SocketTextChannel textChannel && _originalChannelNames.TryGetValue(channelId, out var originalName))
            {
                //await textChannel.ModifyAsync(prop => prop.Name = originalName);
                //_originalChannelNames.Remove(channelId);
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
                var channel = GetChannel(channelId);
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
            if (command.Channel is not SocketTextChannel socketTextChannel)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                return;
            }
            ChannelCache.TryAdd(channelId, socketTextChannel);

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
            if (message.Channel is not SocketTextChannel textChannel) { return; }
            ChannelCache.TryAdd(message.Channel.Id, textChannel);

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
                    Console.WriteLine($"Message sent to Archipelago from {message.Author.Username} in {message.Channel.Name}: {message.Content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
                    QueueMessage(textChannel, "Error: Unable to send message to the Archipelago server.");
                    //await textChannel.SendMessageAsync("Error: Unable to send message to the Archipelago server.");
                }
            }
        }

        private Task LogAsync(LogMessage logMessage)
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
                    if (i.Value.Count == 0) { continue; }
                    List<string> sendQueue = [];
                    int MessageLength = 6; //Include the formatter
                    while (i.Value.Count > 0 && MessageLength < 1500)
                    {
                        MessageLength += i.Value[0].Length + 2; //Include the new line chars that will be added later
                        sendQueue.Add(i.Value[0]);
                        i.Value.RemoveAt(0);
                    }
                    string FormattedMessage = $"```ansi\n{string.Join("\n\n", sendQueue)}\n```";
                    Console.WriteLine($"Sending {sendQueue.Count} Queued messages to channel {i.Key.Name}");
                    await i.Key.SendMessageAsync(FormattedMessage);
                    await Task.Delay(500);
                }
                // No messages to process; wait briefly
                await Task.Delay(500);
            }
        }
    }
}