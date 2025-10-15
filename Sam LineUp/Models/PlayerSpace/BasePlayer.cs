using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.DiscSpace;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace
{
    // ========================= BASE ==========================
    internal abstract class BasePlayer : IPlayer
    {
        public PlayerId Id { get; }
        public PlayerType Type { get; }
        public string Name {get;}

        protected readonly Dictionary<DiscType, int> _bag = new()
        {
            [DiscType.Ordinary] = 0,
            [DiscType.Boring] = 0,
            [DiscType.Exploding] = 0,
            [DiscType.Magnetic] = 0
        };

        private IReadOnlyDictionary<DiscType, int>? _bagView;
        public IReadOnlyDictionary<DiscType, int> AllCounts => _bagView ??= new ReadOnlyDictionary<DiscType, int>(_bag);

        // Constructor
        protected BasePlayer(PlayerId id, PlayerType type, string name)
        {
            Id = id;
            Type = type;
            Name = name;
        }

        // reworked for assessment 2. 
        public void ConfigureForBoard(IBoard board, int GameMode)
        {
            if (board is null) throw new ArgumentNullException(nameof(board));

            // Each player gets half the board's cells (floor).
            int capacity = (board.Rows * board.Cols) / 2;

            if (GameMode == 2) //ie is Line Up Classic
            {
                int ordinary = capacity;
                const int specialsEachKind = 0;

                _bag[DiscType.Ordinary] = ordinary;
                _bag[DiscType.Boring] = specialsEachKind;
                _bag[DiscType.Exploding] = specialsEachKind;
                _bag[DiscType.Magnetic] = specialsEachKind;

            }
            else
            {
                // Exactly 2 boring + 2 exploding + 2 Magnetic; remainder ordinary (never negative).
                const int specialsEachKind = 2;
                int specialsTotal = specialsEachKind * 2;
                int ordinary = Math.Max(0, capacity - specialsTotal);

                _bag[DiscType.Ordinary] = ordinary;
                _bag[DiscType.Boring] = specialsEachKind;
                _bag[DiscType.Exploding] = specialsEachKind;
                _bag[DiscType.Magnetic] = specialsEachKind;

            }


        }

        public int DiscsRemaining =>
            _bag[DiscType.Ordinary] 
            + _bag[DiscType.Boring] 
            + _bag[DiscType.Exploding] 
            + _bag[DiscType.Magnetic];

        public int Count(DiscType type) => _bag[type];

        public IDisc TakeDisc(DiscType type)
        {
            if (_bag[type] <= 0)
                throw new InvalidOperationException($"No {type} discs left for {Name}.");

            _bag[type]--;

            return DiscFactory.Create(Id, type);
        }

        public void Refund(DiscType type, int count = 1)
        {
            _bag[type] += count;
        }

        // Player type implements how to choose a move
        public abstract PlayerDecision ChooseMove(IBoard board);

        // Saving and Loading
        public PlayerState SavePlayer() => new(
            Id,
            Type,
            DiscsRemaining,
            new ReadOnlyDictionary<DiscType, int>(new Dictionary<DiscType, int>(_bag)),
            Name
        );

        public void LoadPlayer(PlayerState state)
        {
            if (state.Id != Id || state.Type != Type)
                throw new InvalidOperationException($"Player identity mismatch. Expected {Id}/{Type}, got {state.Id}/{state.Type}.");

            // Reset and copy counts explicitly
            _bag[DiscType.Ordinary] = state.AllCounts.TryGetValue(DiscType.Ordinary, out var o) ? o : 0;
            _bag[DiscType.Boring] = state.AllCounts.TryGetValue(DiscType.Boring, out var b) ? b : 0;
            _bag[DiscType.Exploding] = state.AllCounts.TryGetValue(DiscType.Exploding, out var x) ? x : 0;
            _bag[DiscType.Magnetic] = state.AllCounts.TryGetValue(DiscType.Magnetic, out var m) ? m : 0;
        }
    }    
    
}
