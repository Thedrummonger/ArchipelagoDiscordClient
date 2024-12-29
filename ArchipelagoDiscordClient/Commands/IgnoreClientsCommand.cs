using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Services;
using ArchipelagoDiscordClient.Settings;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDMUtils;

namespace ArchipelagoDiscordClient.Commands
{
    public class IgnoreClientsCommand : ICommand
    {
        private readonly BotSettings _settings;
        public string CommandName => CommandTypes.IgnoreClientsCommand;

        public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Toggles ignoring Client Messages")
                .AddOption("value", ApplicationCommandOptionType.Boolean, "Value", false)
                .Build();

        public IgnoreClientsCommand(IOptions<BotSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var Value = (bool?)command.Data.Options.FirstOrDefault(option => option.Name == "value")?.Value;
            _settings.IgnoreAllClientMessages = Value ?? !_settings.IgnoreAllClientMessages;
            File.WriteAllText(FilePaths.ConfigFileFullPath, _settings.ToFormattedJson());

            await command.RespondAsync($"Ignoring Client Messages {_settings.IgnoreAllClientMessages}");
        }
    }
}
