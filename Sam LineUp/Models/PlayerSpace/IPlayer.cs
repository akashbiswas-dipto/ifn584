using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.DiscSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace
{
    enum PlayerId { Player1, Player2 }
    enum PlayerType { Human, Computer }

    internal sealed record PlayerDecision(bool Quit, int? Col0, DiscType? Type);

    internal readonly record struct PlayerState(
        PlayerId Id,
        PlayerType Type,
        int DiscsRemaining,
        IReadOnlyDictionary<DiscType, int> AllCounts,
        string Name
    );

    internal interface IPlayerReadonly
    {
        PlayerId Id { get; }
        PlayerType Type { get; }
        string Name { get; }

        // Inventory info
        int DiscsRemaining { get; }
        int Count(DiscType type);                          // remaining of a given type
        IReadOnlyDictionary<DiscType, int> AllCounts { get; }
    }

    internal interface IPlayerCommands
    {
        // Called once when a game starts (sets discs available from board).
        void ConfigureForBoard(IBoard board, int GameMode);

        // Decision hook (Human/AI implements how to pick column & disc type)
        PlayerDecision ChooseMove(IBoard board);

        // Consume one disc of the chosen type and return an instance to drop into the board
        IDisc TakeDisc(DiscType type);

        // Return one disc of the given type back to the player's inventory
        void Refund(DiscType type, int count = 1);

        PlayerState SavePlayer();

        void LoadPlayer(PlayerState state);
    }

    internal interface IPlayer : IPlayerReadonly, IPlayerCommands 
    {
    }
}

