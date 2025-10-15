using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.BoardSpace.ConcreteFactory
{
    public sealed class ClassicBoardFactory: IBoardFactory
    {
        // HARDCODED BOARD SIZE
        public IBoard Create(int rows, int columns) => new Board(8, 9);
    }
}
