using LineUpV3.Models.BoardSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.DiscSpace;
using LineUpV3.Models.SavingSpace;
using System.Text.Json;
using static LineUpV3.UtilSpace.SavingUtils;
using System.Diagnostics.Metrics;

namespace LineUpV3.Models.GameSpace
{
    internal sealed class Game : IGame
    {
        // =============== Setup ================
        public Board Board { get; private set; } = null!;
        public IPlayer Player1 { get; private set; } = null!;
        public IPlayer Player2 { get; private set; } = null!;
        public PlayerId CurrentPlayer { get; private set; }
        public PlayerId NextPlayer { get; private set; }

        public int TurnNumber { get; private set; }

        public PlayerId Winner { get; private set; }

        public GameStatus Status { get; private set; } = GameStatus.NotStarted;

        public int GameMode { get; private set; }

        private IRotation? _rotationStrategy;

        public void SetUp(Board board, IPlayer player1, IPlayer player2, 
                          int gameMode, IRotation? rotationStrategy = null)
        {
            Board = board;
            Player1 = player1;
            Player2 = player2;
            GameMode = gameMode;
            _rotationStrategy = rotationStrategy; // Store strategy for later use

            // set the initial discs
            Player1.ConfigureForBoard(board, GameMode);
            Player2.ConfigureForBoard(board, GameMode);

            // set the starting player
            CurrentPlayer = PlayerId.Player1;
            NextPlayer = PlayerId.Player2;

            Status = GameStatus.InProgress;
            TurnNumber = 0;
        }
        private IPlayer Current => (CurrentPlayer == PlayerId.Player1) ? Player1 : Player2;
        private IPlayer Other => (CurrentPlayer == PlayerId.Player1) ? Player2 : Player1;

        // ============ Game Turn ========

        public bool Turn(IGamePrinter printer)
        {
            FinalState finalState;
            if (TurnNumber == 5 && GameMode == 3)
            {
                TurnNumber = 0;
                // spin the board 90 degrees
                printer.Show(Board, "Before Spin");
                SpinBoard();
               
                printer.Show(Board, "After Spin");

                // Resolve the winner if any
                finalState = GetFinalStateAfterSpin(Current, Other);

                if (UpdateIfFinal(finalState, printer, Current.Id, Current.Name, Other.Id, Other.Name))
                    return false;
            }

            if (Status != GameStatus.InProgress)
                return false;

            var player = Current;

            // Move Decision
            var decision = player.ChooseMove(Board);
            if (decision.Quit)
            {
                printer.Info($"{player.Id} has quit the game.");
                if (ConfirmSave(printer))
                {
                    SavePaths.EnsureDir();
                    printer.Info("Type a name for this save (e.g., 'after move 12'):");

                    Console.Write("Save name: ");
                    var raw = Console.ReadLine() ?? "untitled";
                    var path = SavePaths.MakePath(raw);

                    if (File.Exists(path))
                    {
                        printer.Info($"A save named '{Path.GetFileNameWithoutExtension(path)}' already exists.");
                        Console.Write("Overwrite? (y/n): ");
                        var ans = Console.ReadLine();
                        if (ans == null || !ans.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
                        {
                            // auto-append timestamp to avoid overwrite
                            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            path = SavePaths.MakePath($"{raw}_{stamp}");
                        }
                    }

                    SaveGame.SaveToFile(this, path);
                    printer.Info($"Game saved to '{path}'.");
                }
                return false;
            }

            // check for nulls
            if (decision.Col0 == null || decision.Type == null)
            {
                // check if the board is full. Should be handled by the end of turn check
                // Computer player will return an null move if no valid moves available
                if (Board.IsFull)
                {
                    Status = GameStatus.Finished;
                    printer.Info("Board is full, its a draw.");
                    printer.Show(Board, "Final");
                    return false;
                }
                else if (player.DiscsRemaining == 0)
                {
                    Status = GameStatus.Finished;
                    printer.Info("\nNo discs left to place; it’s a draw.");
                    printer.Show(Board, "Final");
                    return false;
                }
                printer.Info("Invalid move decision (missing column or type). Try again.");
                return true; // stay InProgress; caller will call Turn() again
            }

            int col0 = decision.Col0!.Value;
            var type = decision.Type!.Value;

            // Validate column
            if (col0 < 0 || col0 >= Board.Cols || Board.IsColumnFull(col0))
            {
                printer.Info($"Invalid column {col0 + 1}. Try again.");
                return true; // stay InProgress; caller will call Turn() again
            }

            printer.Info($"\n{player.Name} chooses {type} in column {col0 + 1}.");

            printer.Show(Board, "Before drop");

            // Take disc (bag) and drop
            IDisc disc;
            try
            {
                disc = player.TakeDisc(type);
            }
            catch (InvalidOperationException ex)
            {
                printer.Info(ex.Message);
                return true;
            }

            int row0 = disc.Drop(Board, col0);
            if (row0 < 0)
            {
                // Should not happen, as we checked IsColumnFull above already. This is a backup.
                printer.Info("Column became full unexpectedly. Try another move.");
                return true;
            }

            var before = Board.SaveBoard();

            printer.Show(Board, "After drop");

            // Special effect's resolution, if any
            var (changed, description) = disc.ResolveAfterDrop(Board, row0, col0);
            if (changed)
            {
                var after = Board.SaveBoard();
                if (disc.Type == DiscType.Boring)
                    RefundRemovedDiscs(before, after);

                if (!string.IsNullOrWhiteSpace(description)) printer.Info(description!);
                printer.Show(Board, "After effect");
            }

            finalState = GetFinalStateAfterMove(disc.Type, row0, col0, Current, Other);

            if (UpdateIfFinal(finalState, printer, player.Id, player.Name, Other.Id, Other.Name))
                return false;

            // End-of-turn snapshot
            // printer.Show(Board, "End of turn");

            // Next player + turn count
            CurrentPlayer = NextPlayer;
            NextPlayer = (NextPlayer == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            TurnNumber++;

            return true;
        }

        // ============ Spin Rules =============
        // Execute board rotation using the configured strategy
            private void SpinBoard()
        {
            if (_rotationStrategy == null)
            {
                throw new InvalidOperationException(
                    "Rotation strategy is not configured for this game mode.");
            }

        // Apply the rotation to the board
            Board.ApplyRotation(_rotationStrategy);
        }
        

        // ========== Undo/Redo functions ==========

        // Should be a case of leveraging the existing GameState, to save an array, or maybe dict, with turn and game state. 

        // can then "load" a previous state using the LoadGame functions.

        // Looks like we need to be able to redo, so may need to add a . after the load. 

        // 1.0, 2.0 means initial game
        // 1.1, 2.1 means loaded branch game
        // 1.0, 2.0, 3.0, (UNDO) 2.1, 3.1, 4.1, (UNDO) 1.0, (REDO) 3.0.

        // Can lift code from the save load game logic, and lift printer(board, title) to show the state at each of them.

        // Turns into 5D chess very quickly.


        // ============ Helpers ============

        // Better to do this here, since we can have both states
        private IPlayer PlayerById(PlayerId id) =>
            id == PlayerId.Player1 ? Player1 : Player2;

        // only applying this to Boring currently. But would work for exploding too.
        private void RefundRemovedDiscs(BoardState before, BoardState after)
        {
            var cols = Board.Cols;
            var rows = Board.Rows;
            var bCells = before.Cells;
            var aCells = after.Cells;

            var changedCols = new bool[cols];
            for (int i = 0; i < bCells.Length; i++)
                if (bCells[i] != aCells[i]) changedCols[i % cols] = true;

            for (int i = 0; i < cols; i++)
            {
                // check if the col changed
                if (!changedCols[i])
                {
                    continue;
                }

                // scan the column for counts of each disc, and compare before/after
                var beforeCounts = new Dictionary<(PlayerId, DiscType), int>();
                var afterCounts = new Dictionary<(PlayerId, DiscType), int>();

                for (int r=0; r < rows; r++)
                {
                    int index = r * cols + i;

                    char countB = bCells[index];
                    // count before
                    if (countB != '.' && DiscSymbols.TryParse(countB, out var ownerB, out var typeB))
                    {
                        var keyB = (ownerB, typeB);
                        beforeCounts[keyB] = beforeCounts.GetValueOrDefault(keyB) + 1;
                    }

                    char countA = aCells[index];
                    if (countA != '.' && DiscSymbols.TryParse(countA, out var ownerA, out var typeA))
                    {
                        var keyA = (ownerA, typeA);
                        afterCounts[keyA] = afterCounts.GetValueOrDefault(keyA) + 1;
                    }

                }

                // compare beforeCounts and afterCounts, refunding any missing discs
                foreach (var kv in beforeCounts)
                {
                    var key = kv.Key;
                    int pre = kv.Value;
                    afterCounts.TryGetValue(key, out int post);
                    int removed = pre - post;
                    if (removed > 0)
                    {
                        var (owner, type) = key;
                        PlayerById(owner).Refund(type, removed); // refund to the owner, not current player
                    }
                }
            }

    }

        // Pulling the win/draw logic out of Turn() to make it clearer
        internal enum FinalState { None, CurrentWin, OpponentWin, DoubleWin, Draw, NoDiscsLeft }

        // Helper to determine if the game has reached a terminal (read, we should exit) state after the last move
        private FinalState GetFinalStateAfterMove(
            DiscType lastType, int row0, int col0, IPlayer current, IPlayer other)
        {
            bool currentWon;
            bool otherWon = false;
            if (lastType == DiscType.Ordinary)
            {
                currentWon = Board.IsWinningMove(Board, row0, col0, current.Id);
            }
            else // check whole board for wins
            {
                currentWon = Board.CheckForWin(current.Id);
                otherWon = Board.CheckForWin(other.Id);
            }

            if (currentWon && otherWon)         
                return FinalState.DoubleWin;
            if (currentWon) 
                return FinalState.CurrentWin;
            if (otherWon) 
                return FinalState.OpponentWin;
            if (Board.IsFull) 
                return FinalState.Draw;
            if (current.DiscsRemaining == 0 && other.DiscsRemaining == 0)
                return FinalState.NoDiscsLeft;

            // otherwise, final state not reached, keep going
            return FinalState.None;
        }

        private FinalState GetFinalStateAfterSpin(IPlayer current, IPlayer other)
        {
            bool currentWon = Board.CheckForWin(current.Id);
            bool otherWon = Board.CheckForWin(other.Id);

            // run through the win conditions
            if (currentWon && otherWon)
                return FinalState.DoubleWin;
            if (currentWon)
                return FinalState.CurrentWin;
            if (otherWon)
                return FinalState.OpponentWin;
            if (Board.IsFull)
                return FinalState.Draw;

            return FinalState.None; 
        }

        private bool UpdateIfFinal(
            FinalState s, IGamePrinter printer,
            PlayerId current, string currentName,
            PlayerId other, string otherName)
        {
            switch (s)
            {
                case FinalState.CurrentWin:
                    Winner = current;
                    Status = GameStatus.Finished;
                    printer.Info($"{currentName} wins!");
                    printer.Show(Board, "Final");
                    return true;

                case FinalState.OpponentWin:
                    Winner = other;
                    Status = GameStatus.Finished;
                    printer.Info($"{otherName} wins!");
                    printer.Show(Board, "Final");
                    return true;

                case FinalState.DoubleWin:
                    Status = GameStatus.Finished;
                    printer.Info("Both players have a line; it’s a draw.");
                    printer.Show(Board, "Final");
                    return true;

                case FinalState.Draw:
                    Status = GameStatus.Finished;
                    printer.Info("Board is full; it’s a draw.");
                    printer.Show(Board, "Final");
                    return true;

                case FinalState.NoDiscsLeft:
                    Status = GameStatus.Finished;
                    printer.Info("No discs left for either player; it’s a draw.");
                    printer.Show(Board, "Final");
                    return true;

                default:
                    return false;
            }
        }

        // ======== Saving and Loading ========
        private static bool ConfirmSave(IGamePrinter printer)
        {
            printer.Info("Save game before exiting? (y/n): ");
            string? s = Console.ReadLine();
            return s != null && s.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
        }

        public GameState SaveGameState()
        {
            return new GameState(
                Board.SaveBoard(),
                Player1.SavePlayer(),
                Player2.SavePlayer(),
                CurrentPlayer,         // who’s up now
                GameMode
            );
        }

        public void LoadGameState(GameState s)
        {
            // re-create board if needed
            if (Board == null)
                { Board = new Board(s.Board.Rows, s.Board.Cols);}

            // then refill the board as it was
            Board.LoadBoard(s.Board);

            if ( Player1 == null || Player2 == null)
            {
                // need to re-create the players
                // first check the type to make sure its consistent
                if (s.Player1.Type == PlayerType.Human)
                {
                    Player1 = new HumanPlayer(s.Player1.Id, s.Player1.Name);
                }
                else
                {
                    Player1 = new ComputerPlayer(s.Player1.Id, s.Player1.Name);
                }

                if (s.Player2.Type == PlayerType.Human)
                {
                    Player2 = new HumanPlayer(s.Player2.Id, s.Player2.Name);
                }
                else
                {
                    Player2 = new ComputerPlayer(s.Player2.Id, s.Player2.Name);
                }
            }
            // then refill their inventory to match the saved state
            Player1.LoadPlayer(s.Player1);
            Player2.LoadPlayer(s.Player2);
            CurrentPlayer = s.CurrentPlayerId;
            GameMode = s.GameMode;
            NextPlayer = (CurrentPlayer == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            Status = GameStatus.InProgress;
        }
    }
    
}
