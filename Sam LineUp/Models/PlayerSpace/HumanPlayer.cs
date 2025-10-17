using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.DiscSpace;
using LineUpV3.UtilSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace
{
    internal sealed class HumanPlayer : BasePlayer
    {
        public HumanPlayer(PlayerId id, string name)
                    : base(id, PlayerType.Human, name) { }
        public override PlayerDecision ChooseMove(IBoard board)
        {
            while (true)
            {
                Console.Write($"\n{Name} ({Id}) — column 1..{board.Cols} ([H]elp, [Q]uit, [S]ave, [U]ndo, [R]edo):");
                string? raw = Console.ReadLine()?.Trim();

                // do nothing when null or empty
                if (string.IsNullOrEmpty(raw)) continue;

                switch (raw?.ToUpperInvariant())
                {
                    case "H":// Help
                        HelpText.ShowHelpInTurn();
                        continue; // resets to allow a new input
                    case "Q": // Quit
                        return new PlayerDecision(Col0: null, Type: null, Command: "Q");
                    case "S": //Save
                        return new PlayerDecision(Col0: null, Type: null, Command: "S");
                    case "U": //Undo
                        return new PlayerDecision(Col0: null, Type: null, Command: "U");
                    case "R": //Redo
                        return new PlayerDecision(Col0: null, Type: null, Command: "R");
                }

                if (!int.TryParse(raw, out int col1)) continue;
                int col0 = col1 - 1;
                if (col0 < 0 || col0 >= board.Cols) { Console.WriteLine("Out of range."); continue; }
                if (board.IsColumnFull(col0)) { Console.WriteLine("Column full."); continue; }

                // Disc type
                Console.Write($"\n{Name} - {DiscsRemaining} discs left \nChoose disc type: [|Default| O=Ordinary {_bag[DiscType.Ordinary]}, B=Boring {_bag[DiscType.Boring]}, E=Exploding {_bag[DiscType.Exploding]}, M= Magnetic {_bag[DiscType.Magnetic]}] (H for help):: ");
                string? t = Console.ReadLine()?.Trim().ToUpperInvariant();

                // backup in case of help request at this point
                if (t == "H")
                {
                    HelpText.ShowHelpInTurn();
                    continue;
                }
                bool isValidToken = true;
                DiscType? chosen = t switch
                {
                    "O" => DiscType.Ordinary,
                    "" => DiscType.Ordinary,
                    null => DiscType.Ordinary,
                    "B" => DiscType.Boring,
                    "E" => DiscType.Exploding,
                    "M" => DiscType.Magnetic,
                    // Else change the flag
                    _ => (isValidToken = false, DiscType.Ordinary).Item2
                };
                if (!isValidToken)
                {
                    Console.WriteLine("Invalid input. Please enter O, B, E, or M.");
                    continue;
                }

                if (Count(chosen!.Value) <= 0)
                {
                    Console.WriteLine($"\nNo {chosen} discs left. Try another type.");
                    continue;
                }

                return new PlayerDecision(Col0: col0, Type: chosen);
            }
        }
    }
}