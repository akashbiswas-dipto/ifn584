using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.PlayerSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.DiscSpace
{

    internal sealed class BoringDisc : Disc
    {
        public BoringDisc(PlayerId owner) : base(owner, DiscType.Boring) { }

        public override (bool changed, string? description) ResolveAfterDrop(IBoard board, int row, int col)
        {
            // clear the entire column
            for (int r = 0; r < board.Rows; r++)
                board.SetCell(r, col, null);

            // set self to the bottom of the column
            board.SetCell(board.Rows - 1, col, this);

            return (true, "Boring disc drilled the column; removed discs refunded.");
        }
    }
}
