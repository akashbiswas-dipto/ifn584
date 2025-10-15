using LineUpV3.Models.DiscSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace;

namespace LineUpV3.Models.BoardSpace
{
    // Saving the board state
    internal readonly record struct BoardState(
    int Rows,
    int Cols,
    string Cells,                 // length = Rows*Cols, row-major: '.' empty or a symbol per DiscSymbols
    (int row, int col)? LastMove  // null before first move
    );


    // interface for board commands that modify state
    internal interface IBoardCommands
    {
        // Board command methods
        void ClearBoard();

        /// Direct set for restoring state.
        void SetCell(int row, int col, IDisc? disc);

        BoardState SaveBoard();
        void LoadBoard(BoardState state);

        // Gravity command methods
        void ApplyGravityInColumn(int col);
        void ApplyGravityAll();

        // Rotation command method
        void ApplyRotation(IRotation rotation);
    }
    // interface for read-only board access
    internal interface IBoardReadonly
    {
        int Rows { get; }
        int Cols { get; }
        IDisc? GetCell(int row, int col);
        bool IsColumnFull(int col);          
        bool IsFull { get; }                 
        (int row, int col)? LastMove { get; }

        // For the Computer Player to use to check for winning moves
        int WinLength { get; }
        bool IsWinningMove(IBoardReadonly board, int row, int col, PlayerId player_id);

        bool CheckForWin(PlayerId player_id);

        void PrintBoard();
    }

    // interface for the board itself
    internal interface IBoard : IBoardReadonly, IBoardCommands { }
}
