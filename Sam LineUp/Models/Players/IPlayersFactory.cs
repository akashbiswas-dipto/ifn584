using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace
{
   interface IPlayersFactory {
       (
            IPlayer player1,
            IPlayer player2,
            bool IsTest
       )

       Create(); 
    }
}
