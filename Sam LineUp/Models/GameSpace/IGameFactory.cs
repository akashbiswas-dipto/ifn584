using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.GameSpace
{
    internal interface IGameFactory
    {
        // Rebuild a ready to run game
        Game Build(GameConfig config); 
    }
}
