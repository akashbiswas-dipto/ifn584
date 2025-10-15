using LineUpV3.Models.GameSpace;
using LineUpV3.Models.RunnerSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LineUpV3.Models.RunnerSpace
{

    internal sealed class TestRunner : IGameRunner
    {
        private readonly IGame _game;
        private readonly IGamePrinter _printer;

        public TestRunner(IGame game, IGamePrinter printer)
        {
            _game = game;
            _printer = printer;
        }

        public void Run()
        {
            Console.WriteLine("Enter test moves (e.g., o1,b2,e2,q).");
            Console.WriteLine("Notes:");
            Console.WriteLine(" - Token formats accepted: o1, b2, e3, 1o, 2b, 3e (disc+col OR col+disc).");
            Console.WriteLine(" - Use 'q' to quit.");
            Console.Write("> ");

            var raw = Console.ReadLine() ?? string.Empty;

            // turns this into a script that we feed in as though the user were typing it
            var (scripted, turnCount) = BuildScriptedInput(raw);
            var originalIn = Console.In;

            int turns = 0;

            try
            {
                // redirect console input to our scripted input.
                Console.SetIn(new StringReader(scripted));
                _printer.Show(_game.Board, "Initial");

                while (_game.Status == GameStatus.InProgress && turns < turnCount)
                {
                    Thread.Sleep(500); // to slow it down and make it easier to follow
                    if (!_game.Turn(_printer)) break;
                    turns++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.SetIn(originalIn);
        }

        private static (string ScriptedInput, int TurnCount  ) BuildScriptedInput(string raw)
        {
            // Split on commas or whitespace, ignore empties.
            var tokens = raw.Split(new[] { ',', ' ', '\t', '\r', '\n' },
                                   StringSplitOptions.RemoveEmptyEntries);
            int turncount = 0;
            var sb = new StringBuilder();

            // regex for formats (case-insensitive O/B/E, digits)
            var discCol = new Regex(@"^([oObBeE])(\d+)$");
            var colDisc = new Regex(@"^(\d+)([oObBeE])$");

            // regex for quit
            var quit = new Regex(@"^[qQ]$");


            foreach (var rawTok in tokens)
            {
                var tok = rawTok.Trim();
                if (tok.Length == 0) continue;

                // Check for quit first
                if (quit.IsMatch(tok))
                {
                    sb.AppendLine("q,");
                    sb.AppendLine(",");
                    break; // anything after this doesn't matter, its already over.
                }


                // if the format is disc+col
                var m1 = discCol.Match(tok);
                if (m1.Success)
                {
                    // Invert: feed column then disc
                    var disc = m1.Groups[1].Value; // O/B/E (any case)
                    var col = m1.Groups[2].Value; // digits
                    sb.AppendLine(col);
                    sb.AppendLine(NormaliseDisc(disc));
                    turncount++;
                    continue;
                }

                // if the format is col+disc
                var m2 = colDisc.Match(tok);
                if (m2.Success)
                {
                    var col = m2.Groups[1].Value;
                    var disc = m2.Groups[2].Value;
                    sb.AppendLine(col);
                    sb.AppendLine(NormaliseDisc(disc));
                    turncount++;
                    continue;
                }

                // If it's only digits, guessing the user meant ordinary disc
                if (int.TryParse(tok, out var colOnly))
                {
                    sb.AppendLine(tok);
                    sb.AppendLine("O");
                    turncount++;
                    continue;
                }

                throw new ArgumentException($"Unrecognised token '{tok}'. Use formats like o1, b2, e3, 1o, or 'q'.");
            }

            sb.AppendLine("q");// ensure we always end with quit
            sb.AppendLine(""); // final newline

            return (sb.ToString(), turncount);
        }

        private static string NormaliseDisc(string s)
        {
            char c = char.ToUpperInvariant(s[0]);
            if (c is 'O' or 'B' or 'E' or 'M') return c.ToString();
            throw new ArgumentException($"Invalid disc '{s}'. Use O, B, E or M.");
        }
    }
}
