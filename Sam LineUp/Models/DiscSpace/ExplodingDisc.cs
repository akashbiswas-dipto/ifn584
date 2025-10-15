using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.PlayerSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.DiscSpace
{
    internal sealed class ExplodingDisc : Disc
    {
        public ExplodingDisc(PlayerId owner) : base(owner, DiscType.Exploding) { }

        public override (bool changed, string? description) ResolveAfterDrop(IBoard board, int row, int col)
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int r = row + dr, c = col + dc;
                    if (r >= 0 && r < board.Rows && c >= 0 && c < board.Cols)
                        board.SetCell(r, c, null);
                }

            board.ApplyGravityAll();

            return (true, "Exploding disc detonated and cleared adjacent cells.");
        }
    }
}
