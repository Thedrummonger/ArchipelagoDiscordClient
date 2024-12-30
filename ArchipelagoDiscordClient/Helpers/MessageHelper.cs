using System.Text.RegularExpressions;
using ArchipelagoDiscordClient.Settings;

namespace ArchipelagoDiscordClient.Helpers
{
	public static class MessageHelper
	{
		public static bool CheckMessageTags(this string input, BotSettings settings)
		{
			bool IsClient = input.IsClientNotificationString(out Version version);
			if (!IsClient) { return true; }
			Console.WriteLine($"Client Connecting V{version}");
			if (IsClient && settings.IgnoreAllClientMessages) { return false; }

			/* Not sure if I'll ever use this, but I don't want to write the regex again
            int Team = -1;
            string TeamPattern = @"\(Team #(\d+)\)";
            Match TeamMatch = Regex.Match(input, TeamPattern);
            if (TeamMatch.Success && int.TryParse(TeamMatch.Groups[1].Value, out int ParsedTeam))
                Team = ParsedTeam;
            */

			string TagPattern = @"\[(.*?)\]"; //Tags are defined in brackets
			MatchCollection TagMatches = Regex.Matches(input, TagPattern);
			HashSet<string> tags = [];
			HashSet<string> IgnoredTags = [];
			if (TagMatches.Count > 0)
			{
				string lastMatch = TagMatches[^1].Groups[1].Value; //Tags are always at the end of a message
				string[] parts = lastMatch.Split(',');             //I don't think any brackets would ever appear other than the tags
				foreach (string part in parts)                     //but just pick the last instance of brackets to be safe
				{
					string PartTrimmed = part.Trim();
					if (!PartTrimmed.StartsWith("'") || !PartTrimmed.EndsWith("'")) { continue; } //Probably more unnecessary checking
					string Tag = PartTrimmed[1..^1].ToLower();                                    //But tags are always in single quotes
					tags.Add(Tag);
					if (settings.IgnoreTags.Contains(Tag))
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
        //Checks if the AP message is a client notification
        //Mostly messages about a client connecting or disconnecting
        public static bool IsClientNotificationString(this string message, out Version version)
        {
            string pattern = @"Client\((\d+\.\d+\.\d+)\)"; //Check for string with the client version info.
                                                           //This is consistent for all client connection messages
                                                           //We can assume if it has this string, it a client message
            version = new Version(0, 0, 0);
            Match match = Regex.Match(message, pattern);
            if (match.Success)
            {
                string versionString = match.Groups[1].Value; //Pull out the version from the client info string, currently unused.
                if (Version.TryParse(versionString, out Version? ParsedVersion))
                    version = ParsedVersion;
                return true;
            }
            return false;
        }
    }
}
