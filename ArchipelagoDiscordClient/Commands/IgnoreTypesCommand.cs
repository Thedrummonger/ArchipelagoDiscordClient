using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Settings;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDMUtils;

namespace ArchipelagoDiscordClient.Commands
{
    public class AddIgnoreTypesCommand : ICommand
    {
        private readonly BotSettings _settings;
        public string CommandName => CommandTypes.AddIgnoreTypesCommand;

        public AddIgnoreTypesCommand(IOptions<BotSettings> settings)
        {
            _settings = settings.Value;
        }

        public SlashCommandProperties Properties => new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Add the given types to the ignored types list")
            .AddOption("type", ApplicationCommandOptionType.String, "Type to Add", true)
            .Build();

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var value = (string?)command.Data.Options.FirstOrDefault(option => option.Name == "type")?.Value??"";
            var values = value.TrimSplit(",");
            foreach (var item in values) 
            {
                if (item.IsNullOrWhiteSpace()) continue;
                _settings.IgnoreTags.Add(item.Trim());
            }

            File.WriteAllText(FilePaths.ConfigFileFullPath, _settings.ToFormattedJson());

            await command.RespondAsync($"Added Ignored Types [{string.Join(", ", values)}]");
        }
    }
    public class DelIgnoreTypesCommand : ICommand
    {
        private readonly BotSettings _settings;
        public string CommandName => CommandTypes.DelIgnoreTypesCommand;

        public DelIgnoreTypesCommand(IOptions<BotSettings> settings)
        {
            _settings = settings.Value;
        }

        public SlashCommandProperties Properties => new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Remove the given types from the ignored types list")
            .AddOption("type", ApplicationCommandOptionType.String, "Type to Add", true)
            .Build();

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var value = (string?)command.Data.Options.FirstOrDefault(option => option.Name == "type")?.Value ?? "";
            var values = value.TrimSplit(",");
            foreach (var item in values)
            {
                if (item.IsNullOrWhiteSpace()) continue;
                _settings.IgnoreTags.Remove(item.Trim());
            }

            File.WriteAllText(FilePaths.ConfigFileFullPath, _settings.ToFormattedJson());

            await command.RespondAsync($"Removed Ignored Types [{string.Join(", ", values)}]");
        }
    }
    public class ListIgnoreTypesCommand : ICommand
    {
        private readonly BotSettings _settings;
        public string CommandName => CommandTypes.ListIgnoreTypesCommand;

        public ListIgnoreTypesCommand(IOptions<BotSettings> settings)
        {
            _settings = settings.Value;
        }

        public SlashCommandProperties Properties => new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("List the types in the ignored types list")
            .Build();

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await command.RespondAsync($"Ignored Types [{string.Join(", ", _settings.IgnoreTags)}]");
        }
    }
}
