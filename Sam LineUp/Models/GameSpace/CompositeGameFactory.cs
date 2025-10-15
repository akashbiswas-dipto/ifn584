using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.PlayerSpace;

namespace LineUpV3.Models.GameSpace
{
    public sealed class CompositeGameFactory : IGameFactory
    {
        // individual component factories
        private readonly IBoardFactory _boardFactory;
        private readonly IPlayersFactory _playersFactory;
        private readonly IRotationFactory _rotationFactory;

        // Composite Factory
        public CompositeGameFactory(IBoardFactory boardFactory, IPlayersFactory playersFactory,
            IRotationFactory rotationFactory)
        {
            _boardFactory = boardFactory;
            _playersFactory = playersFactory;
            _rotationFactory = rotationFactory;
        }

        public Game Build(GameConfig gameConfig)
        {
            var board = _boardFactory.Create(gameConfig.Rows, gameConfig.Cols);
            var (player1, player2, isTest) = _playersFactory.Create();
            var rotation = _rotationFactory.Create(gameConfig.GameMode);

            // return the constructed game
            return Game.Create(board, player1, player2, rotation);
        }



    }
}
