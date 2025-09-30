using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.GameSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LineUpV3.Models.RunnerSpace

{
    internal sealed class ConsoleRunner : IGameRunner
    {
        private readonly IGame _game;
        private readonly IGamePrinter _printer;

        public ConsoleRunner(IGame game, IGamePrinter printer)
        {
            _game = game;
            _printer = printer;
        }

        public void Run()
        {
            Console.WriteLine("{0} vs {1}", _game.Player1.Name, _game.Player2.Name);
            Console.WriteLine("{0} in a row to win", _game.Board.WinLength); // Because I kept getting confused when debugging
            _printer.Show(_game.Board, "Initial");
            while (_game.Status == GameStatus.InProgress)
            {
                if (!_game.Turn(_printer)) break;
            }
        }
    }
}
