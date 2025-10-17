using LineUpV3.Models.BoardSpace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.DiscSpace;
using LineUpV3.Models.SavingSpace; // Contains the new static SaveGame class with SaveToFile
using LineUpV3.Models.BoardSpace.ConcreteFactory; // For IRotation
using LineUpV3.UtilSpace; // Required for access to utilities like DiscSymbols

// Assuming the concrete classes Board, HumanPlayer, and ComputerPlayer are accessible
// via the existing using statements or are defined in their respective namespaces.
// We are explicitly using the concrete classes here for deserialization purposes.

namespace LineUpV3.Models.GameSpace
{
    internal sealed class Game : IGame
    {
        // =============== Setup ================
        public IBoard Board { get; private set; } = null!;
        public IPlayer Player1 { get; private set; } = null!;
        public IPlayer Player2 { get; private set; } = null!;
        public PlayerId CurrentPlayer { get; private set; }
        public PlayerId NextPlayer { get; private set; }
        public int TurnNumber { get; private set; }
        public PlayerId Winner { get; private set; }
        public GameStatus Status { get; private set; } = GameStatus.NotStarted;
        public int GameMode { get; private set; }
        private IRotation? _rotationStrategy;
        public bool IsTestMode { get; private set; } = false;

        // ============ Undo/Redo History =============
        private readonly List<GameState> _history = new();
        private int _historyIndex = -1;

        public void InitializeHistoryForNewGame()
        {
            _history.Clear();
            _history.Add(SaveGameState());
            _historyIndex = 0;
        }

        public void InitializeHistoryFromFileLoad(GameState loadedState)
        {
            _history.Clear();

            var previousPlayerId = (loadedState.CurrentPlayerId == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            int previousTurnNumber = loadedState.TurnNumber;

            var dummyState = loadedState with
            {
                CurrentPlayerId = previousPlayerId,
                TurnNumber = previousTurnNumber
            };

            _history.Add(dummyState);
            _history.Add(SaveGameState());

            _historyIndex = 1;
        }

        private void RecordSnapshot()
        {
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
            }
            _history.Add(SaveGameState());
            _historyIndex = _history.Count - 1;
        }

        private bool CanUndo() => _historyIndex > 0;
        private bool CanRedo() => _historyIndex < (_history.Count - 1);

        public bool Undo(IGamePrinter printer)
        {
            if (!CanUndo())
            {
                printer?.Info("Nothing to undo.");
                return false;
            }

            _historyIndex--;
            LoadGameState(_history[_historyIndex]);

            printer?.Info("Undo performed.");
            var waitingPlayer = CurrentPlayer == PlayerId.Player1 ? Player1 : Player2;

            printer?.Info($"The turn returns to {waitingPlayer.Name}. You can type 'REDO' now to re-apply the move you just undid, or make a new move.");
            printer!.Show(Board, $"After Undo (Turn {TurnNumber})");

            return true;
        }

        public bool Redo(IGamePrinter printer)
        {
            if (!CanRedo())
            {
                if (_history.Count > 1 && _historyIndex == _history.Count - 1)
                {
                    printer?.Info("Nothing to redo. A new move was made, which permanently cleared the redo option.");
                }
                else
                {
                    printer?.Info("Nothing to redo.");
                }
                return false;
            }

            _historyIndex++;
            LoadGameState(_history[_historyIndex]);

            printer?.Info("Redo performed.");
            printer!.Show(Board, $"After Redo (Turn {TurnNumber})");

            return true;
        }

        // ============ Create Game ============
        private Game() { }

        public static Game Create(IBoard board, IPlayer player1, IPlayer player2,
                                     int gameMode, IRotation? rotation, bool isTestMode)
        {
            var g = new Game
            {
                Board = board,
                Player1 = player1,
                Player2 = player2,
                GameMode = gameMode,
                CurrentPlayer = PlayerId.Player1,
                NextPlayer = PlayerId.Player2,
                Status = GameStatus.InProgress,
                TurnNumber = 0,
                IsTestMode = isTestMode
            };

            g._rotationStrategy = rotation;
            player1.ConfigureForBoard(board, gameMode);
            player2.ConfigureForBoard(board, gameMode);

            g.InitializeHistoryForNewGame();

            return g;
        }

        private IPlayer Current => (CurrentPlayer == PlayerId.Player1) ? Player1 : Player2;
        private IPlayer Other => (CurrentPlayer == PlayerId.Player1) ? Player2 : Player1;

        // ============ Save Confirmation ============

        /// <summary>
        /// Prompts the user to confirm if they want to save the game.
        /// This is private and only used when 'Q' is entered in the Turn method.
        /// </summary>
        private static bool ConfirmSave(IGamePrinter printer)
        {
            printer.Info("Save game before exiting? (y/n): ");
            string? s = Console.ReadLine();
            return s != null && s.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
        }

        // ============ Turn Logic ============
        public bool Turn(IGamePrinter printer)
        {
            FinalState finalState;

            if (Status != GameStatus.InProgress) return false;

            var player = Current;
            var decision = player.ChooseMove(Board);

            if (decision.Quit)
            {
                if (ConfirmSave(printer))
                {
                    // Prompt user for filename
                    printer.Info("Enter filename to save (default: savegame.txt): ");
                    string? input = Console.ReadLine()?.Trim();

                    string filename = string.IsNullOrEmpty(input) ? "savegame.txt" : input;

                    // Automatically add the .sav extension if the user didn't specify one
                    if (!filename.Contains('.'))
                    {
                        filename += ".txt";
                    }

                    // *** UPDATED TO USE NEW SavingSpace.SaveGame.SaveToFile method ***
                    try
                    {
                        LineUpV3.Models.SavingSpace.SaveGame.SaveToFile(this, filename);
                        printer.Info($"Game saved to '{filename}'.");
                    }
                    catch (IOException ex)
                    {
                        printer.Info($"ERROR: Failed to save game to '{filename}'. Details: {ex.Message}");
                    }
                }
                return false; // Tells the ConsoleRunner to break the loop
            }

            // Handle Undo/Redo commands
            if (decision.Command?.Equals("undo", StringComparison.OrdinalIgnoreCase) == true)
            {
                Undo(printer);
                return true;
            }

            if (decision.Command?.Equals("redo", StringComparison.OrdinalIgnoreCase) == true)
            {
                Redo(printer);
                return true;
            }

            // If no column or type was selected (shouldn't happen with valid HumanPlayer logic)
            if (decision.Col0 == null || decision.Type == null)
            {
                return true;
            }

            int col0 = decision.Col0.Value;
            var type = decision.Type.Value;

            // Start of actual move sequence
            printer.Info($"\n{player.Name} chooses {type} in column {col0 + 1}.");

            // 1. Show Board before drop
            printer.Show(Board, "Before drop");

            // Disc dropping logic
            IDisc disc;
            try { disc = player.TakeDisc(type); }
            catch (InvalidOperationException ex) { printer.Info(ex.Message); return true; }

            int row0 = disc.Drop(Board, col0);
            if (row0 < 0) { printer.Info("Column unexpectedly became full."); return true; }

            var before = Board.SaveBoard();

            // 2. Show Board immediately after drop
            printer.Show(Board, "After drop");

            // Resolution of special disc effects
            var (changed, description) = disc.ResolveAfterDrop(Board, row0, col0);

            if (changed)
            {
                var after = Board.SaveBoard();
                if (disc.Type == DiscType.Boring)
                    RefundRemovedDiscs(before, after);

                if (!string.IsNullOrWhiteSpace(description)) printer.Info(description!);

                // 3. Show Board after effects (Crucial for Exploding disc visualization)
                printer.Show(Board, "After effect");
            }

            finalState = GetFinalStateAfterMove(disc.Type, row0, col0, Current, Other);
            if (UpdateIfFinal(finalState, printer, player.Id, player.Name, Other.Id, Other.Name)) return false;

            if (TurnNumber > 0 && TurnNumber % 5 == 0 && GameMode == 3)
            {
                printer.Show(Board, "Before Spin");
                SpinBoard();
                printer.Show(Board, "After Spin");

                finalState = GetFinalStateAfterSpin(Current, Other);
                if (UpdateIfFinal(finalState, printer, Current.Id, Current.Name, Other.Id, Other.Name)) return false;
            }

            CurrentPlayer = NextPlayer;
            NextPlayer = (NextPlayer == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            TurnNumber++;

            RecordSnapshot();

            return true;
        }

        // ============ Spin Board ============
        private void SpinBoard()
        {
            if (_rotationStrategy == null)
                throw new InvalidOperationException("Rotation strategy not configured.");
            Board.ApplyRotation(_rotationStrategy);
        }

        // ============ Helpers ============

        private IPlayer PlayerById(PlayerId id) => id == PlayerId.Player1 ? Player1 : Player2;

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
                if (!changedCols[i]) continue;
                var beforeCounts = new Dictionary<(PlayerId, DiscType), int>();
                var afterCounts = new Dictionary<(PlayerId, DiscType), int>();

                for (int r = 0; r < rows; r++)
                {
                    int index = r * cols + i;
                    char countB = bCells[index];
                    if (countB != '.' && DiscSymbols.TryParse(countB, out var ownerB, out var typeB))
                        beforeCounts[(ownerB, typeB)] = beforeCounts.GetValueOrDefault((ownerB, typeB)) + 1;

                    char countA = aCells[index];
                    if (countA != '.' && DiscSymbols.TryParse(countA, out var ownerA, out var typeA))
                        afterCounts[(ownerA, typeA)] = afterCounts.GetValueOrDefault((ownerA, typeA)) + 1;
                }

                foreach (var kv in beforeCounts)
                {
                    var key = kv.Key;
                    int pre = kv.Value;
                    afterCounts.TryGetValue(key, out int post);
                    int removed = pre - post;
                    if (removed > 0)
                    {
                        var (owner, type) = key;
                        PlayerById(owner).Refund(type, removed);
                    }
                }
            }
        }

        internal enum FinalState { None, CurrentWin, OpponentWin, DoubleWin, Draw, NoDiscsLeft }

        private FinalState GetFinalStateAfterMove(
            DiscType lastType, int row0, int col0, IPlayer current, IPlayer other)
        {
            bool currentWon;
            bool otherWon = false;
            if (lastType == DiscType.Ordinary)
                currentWon = Board.IsWinningMove(Board, row0, col0, current.Id);
            else
            {
                currentWon = Board.CheckForWin(current.Id);
                otherWon = Board.CheckForWin(other.Id);
            }

            if (currentWon && otherWon) return FinalState.DoubleWin;
            if (currentWon) return FinalState.CurrentWin;
            if (otherWon) return FinalState.OpponentWin;
            if (Board.IsFull) return FinalState.Draw;
            if (current.DiscsRemaining == 0 && other.DiscsRemaining == 0)
                return FinalState.NoDiscsLeft;

            return FinalState.None;
        }

        private FinalState GetFinalStateAfterSpin(IPlayer current, IPlayer other)
        {
            bool currentWon = Board.CheckForWin(current.Id);
            bool otherWon = Board.CheckForWin(other.Id);

            if (currentWon && otherWon) return FinalState.DoubleWin;
            if (currentWon) return FinalState.CurrentWin;
            if (otherWon) return FinalState.OpponentWin;
            if (Board.IsFull) return FinalState.Draw;
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
                    printer.Info("No discs left; it’s a draw.");
                    printer.Show(Board, "Final");
                    return true;
                default:
                    return false;
            }
        }

        public GameState SaveGameState()
        {
            return new GameState(
                Board.SaveBoard(),
                Player1.SavePlayer(),
                Player2.SavePlayer(),
                CurrentPlayer,
                GameMode,
                TurnNumber
            );
        }

        public void LoadGameState(GameState s)
        {
            // CRITICAL FIX 1: Load GameMode first, as player configuration depends on it.
            GameMode = s.GameMode;

            // Assuming LineUpV3.Models.BoardSpace.Board is the concrete class
            if (Board == null)
                Board = new LineUpV3.Models.BoardSpace.Board(s.Board.Rows, s.Board.Cols);

            Board.LoadBoard(s.Board);

            if (Player1 == null || Player2 == null)
            {
                // Assuming HumanPlayer and ComputerPlayer are concrete classes
                Player1 = s.Player1.Type == PlayerType.Human
                    ? new LineUpV3.Models.PlayerSpace.HumanPlayer(s.Player1.Id, s.Player1.Name)
                    : new LineUpV3.Models.PlayerSpace.ComputerPlayer(s.Player1.Id, s.Player1.Name);

                Player2 = s.Player2.Type == PlayerType.Human
                    ? new LineUpV3.Models.PlayerSpace.HumanPlayer(s.Player2.Id, s.Player2.Name)
                    : new LineUpV3.Models.PlayerSpace.ComputerPlayer(s.Player2.Id, s.Player2.Name);

                // CRITICAL FIX 2: Newly instantiated players must be configured for the board and game mode,
                // mirroring the logic in Game.Create, before their specific state is loaded.
                Player1.ConfigureForBoard(Board, GameMode);
                Player2.ConfigureForBoard(Board, GameMode);
            }

            Player1.LoadPlayer(s.Player1);
            Player2.LoadPlayer(s.Player2);
            CurrentPlayer = s.CurrentPlayerId;
            TurnNumber = s.TurnNumber;
            NextPlayer = (CurrentPlayer == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            Status = GameStatus.InProgress;
        }
    }
}
