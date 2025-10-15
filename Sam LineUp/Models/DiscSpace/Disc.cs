using LineUpV3.Models.BoardSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace; 

namespace LineUpV3.Models.DiscSpace
{

    // ========== Base Disc Class ==========
    internal abstract class Disc : IDisc
    {
        public DiscType Type { get; protected set; }
        public PlayerId Owner { get; }
        public char Symbol => DiscSymbols.Get(Owner,Type);

        protected Disc(PlayerId owner, DiscType type)
        {
            Owner = owner;
            Type = type;
        }

        // Default drop: fall to lowest empty cell in the column.
        public virtual int Drop(IBoard board, int col)
        {
            if (board is null) throw new ArgumentNullException(nameof(board));
            if (col < 0 || col >= board.Cols) return -1;

            for (int row = board.Rows - 1; row >= 0; row--)
            {
                if (board.GetCell(row, col) is null)
                {
                    board.SetCell(row, col, this);
                    return row;
                }
            }
            return -1; // column full
        }

        public virtual (bool changed, string? description) ResolveAfterDrop(IBoard board, int row, int col)
        {
            return (false, null); // default: no special effect
        }
    }
}
