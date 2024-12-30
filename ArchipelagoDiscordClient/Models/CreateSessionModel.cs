using Discord.WebSocket;

namespace ArchipelagoDiscordClient.Models
{
	public class CreateSessionModel
	{
		public ulong GuildId { get; set; }
		public ulong ChannelId { get; set; }
		public required string IpAddress { get; set; }
		public long Port { get; set; }
		public required string Game { get; set; }
		public required string Name { get; set; }
		public string? Password { get; set; } = string.Empty;
		public required SocketTextChannel Channel { get; set; }
	}
}
