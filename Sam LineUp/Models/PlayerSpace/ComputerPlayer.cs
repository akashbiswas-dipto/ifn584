using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.DiscSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace
{
    internal sealed class ComputerPlayer : BasePlayer
    {
        private readonly Random _rng = new();
        public ComputerPlayer(PlayerId id, string name)
                    : base(id, PlayerType.Computer, name) { }

        // pick a random disc type, weighted to prefer ordinary
        private DiscType PickRandomDiscType()
        {
            while (true)
            {
                double roll = _rng.NextDouble(); // 0.0 .. 1.0
                DiscType chosen;

                // ~10% chance Boring if any left
                if (roll < 0.1 && Count(DiscType.Boring) > 0)
                    chosen = DiscType.Boring;
                // ~10% chance Exploding if any left
                else if (roll < 0.2 && Count(DiscType.Exploding) > 0)
                    chosen = DiscType.Exploding;
                else if (roll <0.3 && Count(DiscType.Magnetic)> 0)
                    chosen = DiscType.Magnetic;
                // Otherwise, prefer Ordinary if available
                else if (Count(DiscType.Ordinary) > 0)
                    chosen = DiscType.Ordinary;
                // Fallbacks if no ordinary left (should be rare)
                else if (Count(DiscType.Boring) > 0)
                    chosen = DiscType.Boring;
                else if (Count(DiscType.Exploding) > 0)
                    chosen = DiscType.Exploding;
                else if (Count(DiscType.Magnetic) > 0)
                    chosen = DiscType.Magnetic;
                else
                    // should catch this earlier, but in case
                    throw new InvalidOperationException("No discs left to pick.");

                // double triple check that we have one to use
                if (Count(chosen) > 0)
                    return chosen;
                // otherwise loop again
            }
        }

        private bool WouldThisMoveWin(IBoard board, PlayerId me, DiscType type, int col)
        {
            if (col < 0 || col >= board.Cols || board.IsColumnFull(col))
            {
                return false;
            }
            // Create a temporary copy of the board to simulate the move
            var snap = board.SaveBoard();

            // make a new board with the same dimensions as the current board
            var temp = new Board(snap.Rows, snap.Cols);

            // load the snapshot into the temp board
            temp.LoadBoard(snap);

            IDisc disc = DiscFactory.Create(me, type);
            int row = disc.Drop(temp, col);
            if (row < 0)
            {
                return false; // column full or invalid drop
            }

            disc.ResolveAfterDrop(temp, row, col); // apply any special effects

            // if ordinary, can just check the one place
            if (type == DiscType.Ordinary)
            {
                return temp.IsWinningMove(temp, row, col, me);
            }
            else // check whole board for win
            // Can't check for an opponent win, since the player doesn't know about the other player.
            // Optimum could potentially result in a draw
            {
                return temp.CheckForWin(me);
            }
        }

        public override PlayerDecision ChooseMove(IBoard board)
        {
            // start with a sleep to simulate "thinking" and slow down the game
            Console.Write($"\n{Name} - {DiscsRemaining} left — [Ordinary {_bag[DiscType.Ordinary]}, Boring {_bag[DiscType.Boring]}, Exploding {_bag[DiscType.Exploding]}]: ");
            Thread.Sleep(500);

            // build the list of cols to check.
            var openCols = new List<int>(board.Cols);
            for (int c = 0; c < board.Cols; c++)
            {
                if (!board.IsColumnFull(c)) openCols.Add(c);
            }
            if (openCols.Count == 0) // Shouldn't happen but to cover the edge case
            {
                return new PlayerDecision(false, null, null);
            }

            // check what types User still has
            DiscType[] tryTypes = { DiscType.Exploding, DiscType.Boring, DiscType.Magnetic, DiscType.Ordinary };

            foreach (var type in tryTypes)
            {
                if (Count(type) <= 0) continue;

                foreach (int col in openCols)
                {
                    if (WouldThisMoveWin(board, Id, type, col))
                    {
                        return new PlayerDecision(false, col, type);
                    }
                }
            }

            // if no winning move available, pick a random open column
            int pickCol = openCols[_rng.Next(openCols.Count)];

            // pick a random type to drop
            try
            {
                DiscType fallback = PickRandomDiscType();
                return new PlayerDecision(false, pickCol, fallback);

            }
            catch (InvalidOperationException)
            {
                // no discs left, should never happen since we check before calling ChooseMove
                return new PlayerDecision(false, null, null);
            }
        }
    }
}
