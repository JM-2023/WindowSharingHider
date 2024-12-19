using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WindowSharingHider
{
    public static class SettingsManager
    {
        // Use current directory as the config path
        private static string SettingsPath => Path.Combine(Directory.GetCurrentDirectory(), "hiddenWindows.json");

        public static void SaveHiddenWindows(IEnumerable<(string Title, int ProcessId)> windows)
        {
            try
            {
                var json = JsonSerializer.Serialize(windows);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                // Optionally handle logging or inform the user.
                Console.WriteLine("Error saving settings: " + ex.Message);
            }
        }

        public static List<(string Title, int ProcessId)> LoadHiddenWindows()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<List<(string Title, int ProcessId)>>(json);
                }
            }
            catch (Exception ex)
            {
                // Optionally handle logging or inform the user.
                Console.WriteLine("Error loading settings: " + ex.Message);
            }
            return new List<(string, int)>();
        }
    }
}
