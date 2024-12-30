using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClient.Constants
{
    internal static class FilePaths
    {
        public static string ConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
        public static string ConfigFileName = "appsettings.json";
        public static string ConfigFileFullPath = Path.Combine(ConfigFilePath, ConfigFileName);
        public static string BaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
    }
}
