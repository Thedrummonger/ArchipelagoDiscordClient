namespace ArchipelagoDiscordClient.Settings
{
	public class BotSettings
	{
		public HashSet<string> IgnoreTags { get; set; } = ["tracker"];
		public bool IgnoreAllClientMessages { get; set; } = false;
		public int DiscordRateLimitDelay { get; set; } = 500;
        public string BotToken { get; set; } = "";
    }
	public class SettingsFile(BotSettings BotSettings)
	{
		public BotSettings BotSettings { get; set; } = BotSettings;
    }
}
