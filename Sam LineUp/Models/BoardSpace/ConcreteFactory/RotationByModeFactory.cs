using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.BoardSpace.ConcreteFactory
{
    public sealed class RotationByModeFactory: IRotationFactory
    {
        // If spin game mode, then set the rule, else null
        public IRotation? Create(int gameMode) => 
            gameMode == 3 
            ? new ClockwiseRotation() 
            : null;
    }
}
