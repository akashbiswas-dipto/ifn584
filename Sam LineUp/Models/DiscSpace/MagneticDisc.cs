using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.PlayerSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.DiscSpace
{
    internal sealed class MagneticDisc : Disc
    {
        public MagneticDisc(PlayerId owner) : base(owner, DiscType.Magnetic) { }

        public override (bool changed, string? description) ResolveAfterDrop(IBoard board, int row, int col)
        {
            for (int r = row + 1; r < board.Rows; r++)
            {
                var cell = board.GetCell(r, col);
                if (cell == null) // retroactive clearing of warning
                    throw new Exception("Board Cell Null");
                // if its null or not the same player, keep looking
                if (cell.Owner != this.Owner)
                {
                    continue;
                }
                // once one is found, override the current and last cells with each other
                else if (cell.Owner == this.Owner)
                {
                    int up = r - 1;
                    var above = board.GetCell(up, col);

                    board.SetCell(r, col, above);
                    board.SetCell(up, col, cell);

                    return (true, $"Magnetic disc pulled a friendly disc from ({r},{col}) up to ({up},{col}).");
                }
                
            }
            // if it didn't change anything, just move on.
            return (false, null);
        }
    }
}
