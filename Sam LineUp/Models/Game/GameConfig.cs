using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.GameSpace
{
    // Config to store the setup of the game
    public sealed record GameConfig(int Rows, int Cols, int GameMode);

}
