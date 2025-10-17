using LineUpV3.Models.BoardSpace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.DiscSpace;
using LineUpV3.Models.SavingSpace;
using static LineUpV3.UtilSpace.SavingUtils;

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

        // ============ Undo/Redo History (REPLACING OLD FIELDS) =============
        private readonly List<GameState> _history = new();
        private int _historyIndex = -1; // Renamed from _currentIndex

        /// <summary>
        /// Initialize history after game create/load. Starts history with the current state.
        /// </summary>
        private void InitializeHistory()
        {
            _history.Clear();
            _history.Add(SaveGameState());
            _historyIndex = 0;
        }

        /// <summary>
        /// Record a snapshot after a new move. If there are future states (redo), clear them first.
        /// </summary>
        private void RecordSnapshot()
        {
            // If we've undone some moves (historyIndex not at the end), drop future states.
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
            }

            // Append current state and advance index.
            _history.Add(SaveGameState());
            _historyIndex = _history.Count - 1;
        }

        private bool CanUndo() => _historyIndex > 0;
        private bool CanRedo() => _historyIndex < (_history.Count - 1);

        /// <summary>
        /// Undo moves one step (option 2 semantics implemented by capturing snapshots after each turn).
        /// Returns true if undo succeeded.
        /// </summary>
        public bool Undo(IGamePrinter printer)
        {
            if (!CanUndo())
            {
                printer?.Info("Nothing to undo.");
                return false;
            }

            // Move back in history and load that state
            _historyIndex--;
            var s = _history[_historyIndex];

            // NOTE: We MUST load the state BEFORE displaying the board.
            LoadGameState(s);

            printer?.Info("Undo performed.");
            // ADDED: Show the board after undo
            printer?.Show(Board, $"After Undo (Turn {TurnNumber})");

            return true;
        }

        /// <summary>
        /// Redo moves one step. Returns true if redo succeeded.
        /// </summary>
        public bool Redo(IGamePrinter printer)
        {
            if (!CanRedo())
            {
                printer?.Info("Nothing to redo.");
                return false;
            }

            _historyIndex++;
            var s = _history[_historyIndex];

            // NOTE: We MUST load the state BEFORE displaying the board.
            LoadGameState(s);

            printer?.Info("Redo performed.");
            // ADDED: Show the board after redo
            printer?.Show(Board, $"After Redo (Turn {TurnNumber})");

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

            // Initialize undo/redo history with current state (REPLACING OLD SaveSnapshot)
            g.InitializeHistory();

            return g;
        }

        private IPlayer Current => (CurrentPlayer == PlayerId.Player1) ? Player1 : Player2;
        private IPlayer Other => (CurrentPlayer == PlayerId.Player1) ? Player2 : Player1;

        // ============ Turn Logic ============
        public bool Turn(IGamePrinter printer)
        {
            FinalState finalState;

            if (Status != GameStatus.InProgress)
                return false;

            var player = Current;

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
                            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            path = SavePaths.MakePath($"{raw}_{stamp}");
                        }
                    }

                    SaveGame.SaveToFile(this, path);
                    printer.Info($"Game saved to '{path}'.");
                }
                return false;
            }

            // Handle special commands from human input
            if (!string.IsNullOrEmpty(decision.Command))
            {
                var cmd = decision.Command.Trim().ToLowerInvariant();
                if (cmd == "undo")
                {
                    // Undo retains history state for redo
                    Undo(printer);
                    return true; // continue game (do not count as a played turn)
                }
                else if (cmd == "redo")
                {
                    Redo(printer);
                    return true; // continue game
                }
            }

            if (decision.Col0 == null || decision.Type == null)
            {
                if (Board.IsFull)
                {
                    Status = GameStatus.Finished;
                    printer.Info("Board is full, it’s a draw.");
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
                printer.Info("Invalid move decision. Try again.");
                return true;
            }

            int col0 = decision.Col0.Value;
            var type = decision.Type.Value;

            if (col0 < 0 || col0 >= Board.Cols || Board.IsColumnFull(col0))
            {
                printer.Info($"Invalid column {col0 + 1}. Try again.");
                return true;
            }

            printer.Info($"\n{player.Name} chooses {type} in column {col0 + 1}.");
            printer.Show(Board, "Before drop");

            IDisc disc;
            try { disc = player.TakeDisc(type); }
            catch (InvalidOperationException ex)
            {
                printer.Info(ex.Message);
                return true;
            }

            int row0 = disc.Drop(Board, col0);
            if (row0 < 0)
            {
                printer.Info("Column became full unexpectedly.");
                return true;
            }

            var before = Board.SaveBoard();
            printer.Show(Board, "After drop");

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

            if (TurnNumber > 0 && TurnNumber % 5 == 0 && GameMode == 3)
            {
                printer.Show(Board, "Before Spin");
                SpinBoard();
                printer.Show(Board, "After Spin");

                finalState = GetFinalStateAfterSpin(Current, Other);
                if (UpdateIfFinal(finalState, printer, Current.Id, Current.Name, Other.Id, Other.Name))
                    return false;
            }

            CurrentPlayer = NextPlayer;
            NextPlayer = (NextPlayer == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            TurnNumber++;

            // RECORD the snapshot after the move: (REPLACING OLD SaveSnapshot)
            RecordSnapshot();

            return true;
        }

        // ============ Undo / Redo ============
        // The previous simple CanUndo/CanRedo/Undo/Redo/SaveSnapshot methods have been replaced by the
        // more robust ones near the top of the file.

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
                CurrentPlayer,
                GameMode
            );
        }

        public void LoadGameState(GameState s)
        {
            if (Board == null)
                Board = new Board(s.Board.Rows, s.Board.Cols);

            Board.LoadBoard(s.Board);

            if (Player1 == null || Player2 == null)
            {
                Player1 = s.Player1.Type == PlayerType.Human
                    ? new HumanPlayer(s.Player1.Id, s.Player1.Name)
                    : new ComputerPlayer(s.Player1.Id, s.Player1.Name);

                Player2 = s.Player2.Type == PlayerType.Human
                    ? new HumanPlayer(s.Player2.Id, s.Player2.Name)
                    : new ComputerPlayer(s.Player2.Id, s.Player2.Name);
            }

            Player1.LoadPlayer(s.Player1);
            Player2.LoadPlayer(s.Player2);
            CurrentPlayer = s.CurrentPlayerId;
            GameMode = s.GameMode;
            NextPlayer = (CurrentPlayer == PlayerId.Player1) ? PlayerId.Player2 : PlayerId.Player1;
            Status = GameStatus.InProgress;

            // Reset undo/redo history so undo is available (the loaded state is the current snapshot).
            InitializeHistory();
        }
    }
}