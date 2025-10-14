using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.BoardSpace;

namespace LineUpV3.Models.GameSpace
{
    // use this to track where we are in the game
    // could add to this later?
    internal enum GameStatus { NotStarted, InProgress, Finished }

    internal interface IGameReadOnly
    {

        // keep track of players, can use the current/next to manage turns and reload.
        Board Board { get; }
        IPlayer Player1 { get; }
        IPlayer Player2 { get; }
        PlayerId CurrentPlayer { get; }
        PlayerId NextPlayer { get; }
        GameStatus Status { get; }
        int TurnNumber { get; }
        PlayerId Winner { get; }

        int GameMode { get; }


    }

    internal interface IGameCommands
    {
        // Game Commands
        // Setup
        void SetUp(Board board, IPlayer player1, IPlayer player2, int GameMode, IRotation? rotationStrategy = null);

        bool Turn(IGamePrinter printer);

        // will need to save and load the game state
        GameState SaveGameState();

        void LoadGameState(GameState state);
    }

    internal interface IGame : IGameReadOnly, IGameCommands
    {
        // Anything else?
    }

    internal readonly record struct GameState(
        BoardState Board,
        PlayerState Player1,
        PlayerState Player2,
        PlayerId CurrentPlayerId,
        int GameMode
        );

    // little interface for printing the game state
    internal interface IGamePrinter
    {
        void Info(string message);
        void Show(IBoard board, string? title = null);
    }

}
