using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LineUpV3.UtilSpace
{
    public class SavingUtils
    {
        public static class SavePaths
        {
            public static readonly string SavesDir = Path.Combine(AppContext.BaseDirectory, "saves");

            public static void EnsureDir() => Directory.CreateDirectory(SavesDir);

            public static string Sanitize(string name)
            {
                // Remove anything that isn't letter/number/space/underscore/dash etc
                var cleaned = Regex.Replace(name.Trim(), @"[^A-Za-z0-9 _\-]+", "");
                return string.IsNullOrWhiteSpace(cleaned) ? "untitled" : cleaned;
            }

            public static string MakePath(string name) =>
                Path.Combine(SavesDir, Sanitize(name) + ".txt");
        }

        public static FileInfo[] ListSaves()
        {
            SavePaths.EnsureDir();
            var di = new DirectoryInfo(SavePaths.SavesDir);
            return di.Exists
                ? di.GetFiles("*.txt").OrderByDescending(f => f.LastWriteTimeUtc).ToArray()
                : Array.Empty<FileInfo>();
        }

        public static string? PickSavePath()
        {
            var saves = ListSaves();
            if (saves.Length == 0)
            {
                Console.WriteLine("No saves found.");
                return null;
            }

            Console.WriteLine("\nAvailable saves:");
            for (int i = 0; i < saves.Length; i++)
            {
                var name = Path.GetFileNameWithoutExtension(saves[i].Name);
                Console.WriteLine($"{i + 1}. {name}  (updated {saves[i].LastWriteTime:g})");
            }
            Console.WriteLine("Type a number to load, or type a name to load that file directly.");
            Console.Write("Choice: ");
            var input = Console.ReadLine();

            if (int.TryParse(input, out int idx) && idx >= 1 && idx <= saves.Length)
                return saves[idx - 1].FullName;

            if (!string.IsNullOrWhiteSpace(input))
            {
                var path = SavePaths.MakePath(input!);
                if (File.Exists(path)) return path;
                Console.WriteLine("No save by that name.");
            }
            return null;
        }
    }
}
