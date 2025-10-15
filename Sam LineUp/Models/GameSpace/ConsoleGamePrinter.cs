using LineUpV3.Models.BoardSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.GameSpace
{
    internal sealed class ConsoleGamePrinter : IGamePrinter
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Show(IBoard board, string? title = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Console.WriteLine($"=== {title} ===");
            board.PrintBoard();
            Console.WriteLine();
        }
    }
}
