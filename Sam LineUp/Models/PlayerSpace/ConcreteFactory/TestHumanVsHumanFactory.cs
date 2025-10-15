using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.PlayerSpace.ConcreteFactory
{
    internal sealed class TestHumanVsHumanFactory: IPlayersFactory
    {
        public (IPlayer player1, IPlayer player2, bool isTest) Create() =>
            (new ComputerPlayer(PlayerId.Player1, "Test1"),
            new ComputerPlayer(PlayerId.Player2, "Test2"),
            true);
    }
}
