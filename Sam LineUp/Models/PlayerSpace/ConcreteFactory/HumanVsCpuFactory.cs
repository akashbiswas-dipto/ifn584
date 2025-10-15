using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace.ConcreteFactory
{
    internal sealed class HumanVsCpuFactory: IPlayersFactory
    {
        public (IPlayer player1, IPlayer player2, bool isTest) Create()
            => (new HumanPlayer(PlayerId.Player1, "P1"),
                new ComputerPlayer(PlayerId.Player2, "CPU"),
                false);
    }
}
