using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace.ConcreteFactory
{
    public sealed class HumanVsCpuFactory: IPlayersFactory
    {
        public (IPlayer, IPlayer, bool) Create =>
            (new ComputerPlayer(PlayerId.Player1, "CPU1"),
            new ComputerPlayer(PlayerId.Player2, "CPU2"),
            false);
    }
}
