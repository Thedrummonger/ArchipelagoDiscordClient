using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClient.Constants
{
    internal static class FilePaths
    {
        public static string BaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
        public static string ConfigFileFullPath = Path.Combine(BaseFilePath, FileNames.ConfigurationFileName);
    }
}
