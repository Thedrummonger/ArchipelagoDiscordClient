namespace ArchipelagoDiscordClient.Settings
{
	public class BotSettings
	{
		public HashSet<string> IgnoreTags { get; set; } = new HashSet<string>();
		public bool IgnoreAllClientMessages { get; set; }
		public int DiscordRateLimitDelay { get; set; }
	}
}
