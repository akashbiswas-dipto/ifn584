using LineUpV3.Models.DiscSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace;

namespace LineUpV3.Models.BoardSpace
{

    internal sealed class Board : IBoard
    {
        // =========== Setup =============
        // Board dimensions
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        private IDisc?[,] _grid;

        public int WinLength => ComputeWinLength(Rows, Cols);

        public (int row, int col)? LastMove { get; private set; }

        // Constructor
        public Board(int rows = 6, int cols = 7)
        {
            if (rows < 6 || cols < 7) 
                throw new ArgumentOutOfRangeException(
                    "Board must be at least 6x7 in size.");
            Rows = rows;
            Cols = cols;

            _grid = new IDisc?[rows, cols];
            LastMove = null;
        }

        // ========= Edit Commands ===========
        public void SetCell(int row, int col, IDisc? disc)
        {
            _grid[row, col] = disc;
            LastMove = (row, col);
        }

        public void ClearBoard()
        {
            for (int r=0; r < Rows; r++)
                for (int c=0; c < Cols; c++)
                    _grid[r, c] = null;
            LastMove = null;
        }

        // Gravity Commands (for changes after special disc effects)
        // Good for Assignment 2 later
        public void ApplyGravityInColumn(int col)
        {
            int write = Rows - 1;
            for (int r = Rows - 1; r >= 0; r--)
            {
                var d = GetCell(r, col);
                if (d is not null)
                {
                    if (write != r)
                    {
                        SetCell(write, col, d);
                        SetCell(r, col, null);
                    }
                    write--;
                }
            }
        }

        public void ApplyGravityAll()
        {
            for (int c = 0; c < Cols; c++) ApplyGravityInColumn(c);
        }

        // =========== Save/Load Commands =============
        public BoardState SaveBoard()
        {
            var chars = new char[Rows * Cols];
            int k = 0;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    chars[k++] = _grid[r, c]?.Symbol ?? '.';

            return new BoardState(Rows, Cols, new string(chars), LastMove);
        }

        public void LoadBoard(BoardState s)
        { 
            if (s.Cells is null || s.Cells.Length != Rows * Cols)
                throw new ArgumentException("Snapshot cells length invalid.");

            // Clear current Board (just in case)
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _grid[r, c] = null;

            // Rebuild from snapshot
            int k = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++, k++)
                {
                    char ch = s.Cells[k];
                    if (ch == '.') { _grid[r, c] = null; continue; }

                    if (!DiscSymbols.TryParse(ch, out var owner, out var type))
                        throw new InvalidOperationException($"Unknown disc symbol '{ch}' in snapshot.");

                    _grid[r, c] = DiscFactory.Create(owner, type);
                }
            }

            LastMove = s.LastMove;
        }

        // ======== Read Commands ===========
        public IDisc? GetCell(int row, int col) => _grid[row, col];

        public bool IsColumnFull(int col)
        {
            if (col < 0 || col >= Cols) 
                throw new ArgumentOutOfRangeException(nameof(col));
            // column is full if top cell is occupied
            return _grid[0, col] != null;
        }

        public bool IsFull // check for draw
        {
            get
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (!IsColumnFull(col))
                        return false;
                }
                return true;
            }
        }

        private static int ComputeWinLength(int rows, int cols)
        {
            // 10% of cells (Rounded Down), but never less than 4 (per the baseline). 
            int byProvidedFormula = (int)Math.Floor(rows * cols * 0.10);
            const int baseline6x7 = 4;
            return Math.Max(baseline6x7, byProvidedFormula);
        }

        public bool IsWinningMove(Board board, int row, int col, PlayerId playerId)
        {
            // Guard: valid cell and disc belongs to player
            if (row < 0 || row >= board.Rows || col < 0 || col >= board.Cols)
                return false;

            IDisc? start = board.GetCell(row, col);
            if (start == null || start.Owner != playerId)
                return false;

            // Directions to scan (dr, dc)
            int[][] directions = new int[][]
            {
                new int[] { 0, 1 },  // horizontal (right/left)
                new int[] { 1, 0 },  // vertical (down/up)
                new int[] { 1, 1 },  // diagonal down-right / up-left
                new int[] { 1, -1 }  // diagonal down-left  / up-right
            };

            // For each direction, count both ways and include the starting disc
            for (int i = 0; i < directions.Length; i++)
            {
                int dr = directions[i][0];
                int dc = directions[i][1];

                int count = 1; // start with the disc at (row,col)

                // forward direction (dr, dc)
                count += CountInDirection(board, row, col, dr, dc, playerId);

                // backward direction (-dr, -dc)
                count += CountInDirection(board, row, col, -dr, -dc, playerId);

                if (count >= WinLength)
                    return true;
            }

            return false;
        }

        // Counts continous discs owned by playerId starting *next* cell from (row,col)
        // moving stepwise by (dr,dc) until it hits a boundary or a different players disc.
        private static int CountInDirection(Board board, int row, int col, int dr, int dc, PlayerId playerId)
        {
            int r = row + dr;
            int c = col + dc;
            int n = 0;

            while (r >= 0 && r < board.Rows && c >= 0 && c < board.Cols)
            {
                var d = board.GetCell(r, c);
                if (d == null || d.Owner != playerId)
                    break;

                n++;
                r += dr;
                c += dc;
            }

            return n;
        }

        public bool CheckForWin(PlayerId pid)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    var d = GetCell(r, c);
                    if (d != null && d.Owner == pid && IsWinningMove(this, r, c, pid))
                        return true;
                }
            return false;
        }
        public void PrintBoard()
        {
            for (int r = 0; r < Rows; r++)

            {
                // Left border
                Console.Write($" {Rows-r} |"); // inverted order for display per Assignment
                for (int c = 0; c < Cols; c++)
                {
                    var Disc = GetCell(r,c);
                    char ch = Disc?.Symbol ?? ' ';
                    Console.Write($" {ch} |");
                }
                Console.WriteLine();
            }
            Console.Write("   |");
            for (int c = 0; c < Cols; c++)
            {
                Console.Write($" {c+1} |");
            }
        }
        public void RotateBoard() {
            int newcol = Rows;
            int newrow = Cols;
            IDisc?[,] NewGrid = new IDisc?[newrow, newcol];
            for(int r = 0;r < Rows; r++) {
                for (int c = 0;c < Cols; c++) {
                    NewGrid[c, Rows-r-1] = _grid[r,c]; 
                }
            }
            _grid = NewGrid;
            Rows = newrow;
            Cols = newcol;
            ApplyGravityAll();
            LastMove = null;

    }
}
}
