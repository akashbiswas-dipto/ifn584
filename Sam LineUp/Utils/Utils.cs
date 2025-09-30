using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LineUpV3.UtilSpace
{
    // dropping here for now, not sure where else to put
    public class Utils
    {
        public static bool YesNo(string msg)
        {
            Console.Write(msg + " (y/n): ");
            var s = Console.ReadLine();
            return s != null && s.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
        }

        public static int PromptInt(string message, int min, int? max, int defaultValue)
        {
            string rangeText = max.HasValue ? $"{min}-{max.Value}" : $"{min}+";
            while (true)
            {
                // swap out message based on whether max is set
                Console.Write($"{message} ({rangeText}, default {defaultValue}): ");

                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return defaultValue;
                }
                    

                if (int.TryParse(input, out int value) &&
                    value >= min && (!max.HasValue || value <= max.Value))
                {
                    return value;
                }

                Console.WriteLine($"Invalid input. Please enter a number {rangeText} or press enter to use defaults.");

                continue;
            }
        }

        public static int PromptGameMode()
        {
            Console.WriteLine("\n Choose Game Mode:");
            Console.WriteLine("1. Line Up (w all discs)");
            Console.WriteLine("2. Line Up Classic (base discs)");
            Console.WriteLine("3. Line Up Spin (twister mode)");
            Console.WriteLine();
            var input = PromptInt("Enter Choice", 1, 3, 1);
            if (input == 3) // Drop this once Spin implemented
            {
                throw new NotImplementedException();
            }
            return input;
        }

        public static int PromptRunMode()
        {
            Console.WriteLine("\n Choose Run Mode:");
            Console.WriteLine("1. HvH - Two Human players");
            Console.WriteLine("2. HvC - One Human player vs one computer player");
            Console.WriteLine("3. CvC - Two Computer players");
            Console.WriteLine("4. Test Mode (set of predefined moves)");
            Console.WriteLine();
            var input = PromptInt("Enter choice", 1, 4, 1);
            return input;
        }

        public static (int width, int height) PromptGameSettings()
        {
            while (true)
            {
                int width = PromptInt("Enter board width", 7, null, 7);
                int height = PromptInt("Enter board height", 6, null, 6);

                if (height > width)
                {
                    Console.WriteLine("Board cannot have more rows than columns. Please try again.");
                    continue;
                }
                if (width < 7 || height < 6)
                {
                    Console.WriteLine("Board must be at least 7 columns wide and 6 rows high. Please try again.");
                    continue;
                }

                else
                {
                    return (width, height);
                }
            }
        }
    }
}
