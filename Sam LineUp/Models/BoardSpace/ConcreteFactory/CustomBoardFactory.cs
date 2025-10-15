using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.BoardSpace.ConcreteFactory
{
    public sealed class CustomBoardFactory: IBoardFactory
    {
        public IBoard Create(int rows, int columns) => new Board(rows, columns);
    }
}
