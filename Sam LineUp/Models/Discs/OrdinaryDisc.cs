using LineUpV3.Models.PlayerSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.DiscSpace
{
    internal sealed class OrdinaryDisc : Disc
    {
        public OrdinaryDisc(PlayerId owner) : base(owner, DiscType.Ordinary) { }
    }
}
