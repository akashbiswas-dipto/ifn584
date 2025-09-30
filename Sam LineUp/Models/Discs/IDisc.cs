using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.PlayerSpace;

namespace LineUpV3.Models.DiscSpace
{
    internal enum DiscType
    {
        Ordinary, // Basic
        Boring, // Drill Disc
        Exploding // Bomb Disc

    }

    interface IDisc
    {
        // Disc properties and base methods
        DiscType Type { get; }

        PlayerId Owner { get; }

        char Symbol { get; }

        int Drop(IBoard board, int col);

        // for special disc types that do something *after* being dropped
        (bool changed, string? description) ResolveAfterDrop(IBoard board, int row, int col);
    }
}
