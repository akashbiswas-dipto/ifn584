using LineUpV3.Models.PlayerSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineUpV3.Models.DiscSpace
{

    // come back and force all discs to be created this way, rather than just instatiating directly
    internal static class DiscFactory
    {
        public static IDisc Create(PlayerId owner, DiscType type) => type switch
        {
            DiscType.Ordinary => new OrdinaryDisc(owner),
            DiscType.Boring => new BoringDisc(owner),
            DiscType.Exploding => new ExplodingDisc(owner),
            DiscType.Magnetic => new MagneticDisc(owner),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
