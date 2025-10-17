using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LineUpV3.UtilSpace
{
    public static class SavingUtils
    {
        // Handles save directory and paths
        public static class SavePaths
        {
            public static readonly string SavesDir = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "saves");

            static SavePaths() => Directory.CreateDirectory(SavesDir);

            public static string Sanitize(string name)
            {
                var cleaned = Regex.Replace(name.Trim(), @"[^A-Za-z0-9 _\-]+", "");
                return string.IsNullOrWhiteSpace(cleaned) ? "savegame" : cleaned;
            }

            public static string MakePath(string name) {

                var sanitized = Sanitize(name);

                // Avoid double ".txt"
                if (sanitized.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    return Path.Combine(SavesDir, sanitized);

                return Path.Combine(SavesDir, sanitized + ".txt");
            }
        }

        // Lists all save files
        public static string[] ListSaves()
        {
            Directory.CreateDirectory(SavePaths.SavesDir);
            return Directory.GetFiles(SavePaths.SavesDir, "*.txt")
                            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                            .ToArray();
        }

        // Prompts user to pick a save file
        public static string? PickSavePath()
        {
            var saves = ListSaves();
            if (saves.Length == 0)
            {
                Console.WriteLine("No saves found. Start a new game first.");
                Console.ReadLine();
                return null;
            }

            while (true)
            {
                Console.WriteLine("\nAvailable saves:");
                for (int i = 0; i < saves.Length; i++)
                    Console.WriteLine($"{i + 1}. {Path.GetFileNameWithoutExtension(saves[i])} (updated {File.GetLastWriteTime(saves[i]):g})");

                Console.WriteLine("Type a number, name, or [C] to cancel:");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("C", StringComparison.OrdinalIgnoreCase))
                    return null;

                // Choose by index
                if (int.TryParse(input, out int idx))
                {
                    if (idx >= 1 && idx <= saves.Length)
                        return saves[idx - 1];

                    Console.WriteLine($"Invalid index. Enter 1-{saves.Length}.");
                    continue;
                }

                // Choose by name
                string path = SavePaths.MakePath(input!);
                if (File.Exists(path)) return path;

                Console.WriteLine($"No save found with name: '{input}'");
            }
        }
    }
}
