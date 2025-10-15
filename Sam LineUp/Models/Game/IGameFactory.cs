using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.GameSpace
{
    public interface IGameFactory
    {
        // Rebuild a ready to run game
        Game Build(GameConfig config); 
        // to swap to test mode if needed
        bool IsTestMode {  get; }
    }
}
