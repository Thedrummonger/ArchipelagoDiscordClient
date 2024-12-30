namespace ArchipelagoDiscordClient.Models
{
	public class AssignUserToPlayerModel
	{
		public required string UserId { get; set; }

		public required string Username { get; set; }

		public required string Discriminator { get; set; }

		public required List<string> Players { get; set; } = new List<string>();
	}
}
