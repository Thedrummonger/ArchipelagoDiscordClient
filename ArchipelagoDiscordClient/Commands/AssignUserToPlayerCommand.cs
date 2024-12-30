using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClient.Constants;
using ArchipelagoDiscordClient.Models;
using ArchipelagoDiscordClient.Services;
using Discord;
using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Commands
{
	internal class AssignUserToPlayerCommand : ICommand
	{
		private readonly Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> _activeSessions;
		private readonly IFileService _fileService;

		public string CommandName => CommandTypes.AssignUserToPlayerCommand;

		public SlashCommandProperties Properties => new SlashCommandBuilder()
				.WithName(CommandName)
				.WithDescription("Assign discord user to archipelago player")
				.AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
				.AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true)
				.Build();

		public AssignUserToPlayerCommand(
			Dictionary<ulong, Dictionary<ulong, ArchipelagoSession>> activeSessions,
			IFileService fileService)
		{
			_activeSessions = activeSessions;
			_fileService = fileService;
		}

        public async Task ExecuteAsync(SocketSlashCommand command)
		{
			Utility.GetCommandData(command, out ulong guildId, out ulong channelId, out string channelName, out SocketTextChannel? socketTextChannel);
			if (socketTextChannel is null)
			{
				await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
				return;
			}

			if (!_activeSessions.TryGetValue(guildId, out var guildSessions) || !guildSessions.TryGetValue(channelId, out var session))
			{
				await command.RespondAsync("No active Archipelago session in this channel.", ephemeral: true);
				return;
			}

			var user = command.Data.Options.FirstOrDefault(option => option.Name == "user")?.Value as SocketUser;
			var players = command.Data.Options.FirstOrDefault(option => option.Name == "players")?.Value as string;
			if(!await IsInputValid(command, user, players))
			{
				return;
			}

			var assignUserToPlayerModel = new AssignUserToPlayerModel
			{
				UserId = user!.Id.ToString(),
				Username = user.Username,
				Discriminator = user.Discriminator,
				Players = players!.Split(',').Select(p => p.Trim()).ToList()
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
