﻿using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Helpers;
using ArchipelagoDiscordClient.Models;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;
using TDMUtils;

namespace ArchipelagoDiscordClient.Commands
{
    public class AssignUserToPlayerCommand : ICommand
	{
		private readonly IArchipelagoSessionService _sessionService;
		private readonly IFileService _fileService;

		public string CommandName => CommandTypes.AssignUserToPlayerCommand;

		public SlashCommandProperties Properties => new SlashCommandBuilder()
				.WithName(CommandName)
				.WithDescription("Assign discord user to archipelago player")
				.AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
				.AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true)
				.Build();

		public AssignUserToPlayerCommand(
			IArchipelagoSessionService sessionService,
			IFileService fileService)
		{
			_sessionService = sessionService;
			_fileService = fileService;
		}

        public async Task ExecuteAsync(SocketSlashCommand command)
		{
            var commandData = command.GetCommandData();
			if (commandData.socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}

			var session = _sessionService.GetActiveSessionByChannelIdAsync(commandData.guildId, commandData.channelId);
			if (session == null)
			{
				await command.RespondAsync("No active Archipelago session in this channel.", ephemeral: true);
				return;
			}

			var user = commandData.GetArg("user")?.Value as SocketUser;
			var players = commandData.GetArg("players")?.Value as string;
			if(!await IsInputValid(command, user, players))
			{
				return;
			}

			var assignUserToPlayerModel = new AssignUserToPlayerModel
			{
				UserId = user!.Id.ToString(),
				Username = user.Username,
				Discriminator = user.Discriminator,
				Players = players!.TrimSplit(",").ToList()
			};

			var sessionPlayers = session.Players.AllPlayers.Select(player => player.Name).ToList();
			var invalidPlayers = assignUserToPlayerModel.Players
				.Where(player => !sessionPlayers.Contains(player))
				.ToList();

			if (invalidPlayers.Any())
			{
				await command.RespondAsync($"The following players are invalid: {string.Join(", ", invalidPlayers)}. Please provide valid player names.", ephemeral: true);
				return;
			}

			Console.WriteLine($"Assigning {user} to {players}");
			await command.RespondAsync($"Assigning {user} to {players}...");

			try
			{
				var updatedUserData = await GetUserDataAsync();
				UpdateUserData(ref updatedUserData, assignUserToPlayerModel);
				await _fileService.WriteFileAsync(FileNames.UserDataFileName, updatedUserData);
				Console.WriteLine($"{user} assigned to {players} successfully");
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"{user} assigned to {players} successfully");
			}
			catch
			{
				Console.WriteLine($"Something went wrong. Failed to assign {user} to {players}");
				await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Something went wrong. Failed to assign {user} to {players}");
				throw;
			}
		}

		private async Task<bool> IsInputValid(
			SocketSlashCommand command, 
			SocketUser? user, string? 
			players)
		{
			if (user is null)
			{
				await command.RespondAsync("Invalid format! 'user' field must not be empty", ephemeral: true);
				return false;
			}

			if (string.IsNullOrEmpty(players))
			{
				await command.RespondAsync("Invalid format! 'players' field can not be empty", ephemeral: true);
				return false;
			}

			if (!IsPlayersFormatValid(players))
			{
				await command.RespondAsync("Invalid format! 'players' field should be correctly formatted, e.g, Player1;Player2;Player3", ephemeral: true);
				return false;
			}

			return true;
		}

		private static bool IsPlayersFormatValid(string players)
		{
			var regex = new Regex(@"^(\w+(\s*\w+)*)(,(\w+(\s*\w+)*))*$");
			if (!regex.IsMatch(players))
			{
				return false;
			}

			return true;
		}

		private async Task<List<AssignUserToPlayerModel>> GetUserDataAsync()
		{
			var userData = await _fileService.ReadFileAsync<List<AssignUserToPlayerModel>>(FileNames.UserDataFileName);
			return userData ?? new List<AssignUserToPlayerModel>();
		}

		private void UpdateUserData(ref List<AssignUserToPlayerModel> userData, AssignUserToPlayerModel newUser)
		{
			var existingUser = userData.FirstOrDefault(u => u.UserId == newUser.UserId);

			if (existingUser != null)
			{
				foreach (var player in newUser.Players)
				{
					if (!existingUser.Players.Contains(player))
					{
						existingUser.Players.Add(player);
					}
				}
			}
			else
			{
				userData.Add(newUser);
			}
		}
	}
}
