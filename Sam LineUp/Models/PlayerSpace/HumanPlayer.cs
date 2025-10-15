using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.DiscSpace;
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
                Console.Write($"\n{Name} ({Id}) — column 1..{board.Cols} (Q to quit): ");
                string? raw = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(raw)) continue;
                if (raw.Equals("Q", StringComparison.OrdinalIgnoreCase))
                    return new PlayerDecision(Quit: true, Col0: null, Type: null);

                if (!int.TryParse(raw, out int col1)) continue;
                int col0 = col1 - 1;
                if (col0 < 0 || col0 >= board.Cols) { Console.WriteLine("Out of range."); continue; }
                if (board.IsColumnFull(col0)) { Console.WriteLine("Column full."); continue; }

                // Disc type
                Console.Write($"\n{Name} - {DiscsRemaining} discs left \nChoose disc type: [O=Ordinary {_bag[DiscType.Ordinary]}, B=Boring {_bag[DiscType.Boring]}, E=Exploding {_bag[DiscType.Exploding]}, M= Magnetic {_bag[DiscType.Magnetic]}]: ");
                string? t = Console.ReadLine()?.Trim().ToUpperInvariant();
                DiscType chosen = t switch
                {
                    "B" => DiscType.Boring,
                    "E" => DiscType.Exploding,
                    "M" => DiscType.Magnetic,
                    _ => DiscType.Ordinary
                };

                if (Count(chosen) <= 0)
                {
                    Console.WriteLine($"\nNo {chosen} discs left. Try another type.");
                    continue;
                }

                return new PlayerDecision(Quit: false, Col0: col0, Type: chosen);
            }
        }
    }
}
