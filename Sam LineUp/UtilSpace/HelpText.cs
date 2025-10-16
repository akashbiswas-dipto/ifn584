using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.UtilSpace
{
    internal static class HelpText
    {
        public static void ShowHelpInMain()
        {
            Console.WriteLine(@"
                ========= Help ========
                Goal: Get 10% of the board in a row (horizontal, vertical, or diagonal).

                Game Modes:
                  1) Line Up — all special discs enabled
                  2) Basic — only ordinary discs on a predefined board (8 by 9)
                  3) Spin — every five drops, the board spins 90 degrees. 

                Run Modes:
                  1) Human vs Human
                  2) Human vs CPU
                  3) CPU vs CPU
                  4) Test Mode

                Controls:
                  - Main menu: N=new, L=load, H=help, Q=quit
                  - In game:
                      • At column prompt: enter 1..Cols, H for help, Q to quit
                      • Disc types (if enabled): O=Ordinary - Default, B=Boring, E=Exploding, M=Magnetic

                Special Discs:
                  - Boring: drills the column; removed discs are refunded to their players
                  - Exploding: clears a 3x3 around impact, then gravity drops as needed
                  - Magnetic: after landing, pulls the first friendly up one cell
                "
                );
            Console.WriteLine("Press any key to contrinue");
            Console.ReadLine();

        }

        public static void ShowHelpInTurn()
        {
            Console.WriteLine(@"
            ====== Turn Help ========
            - Enter a column number (1..Cols) to drop a disc.
            - H to see this help, Q to quit.
                - After Quitting, there will be an option to save the game, Y/N defaulting to N.
                - If the game is saved, a name can be given to the save, to find later.
            - Disc types:
                O=Ordinary, B=Boring, E=Exploding, M=Magnetic (if available)
            "
            );
            Console.WriteLine();
        }
    }
}
