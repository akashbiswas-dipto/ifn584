using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace;

namespace LineUpV3.Models.DiscSpace
{
    internal static class DiscSymbols
    {
        private static readonly Dictionary<(PlayerId, DiscType), char> _glyphs =
            new Dictionary<(PlayerId, DiscType), char>
            {
            { (PlayerId.Player1, DiscType.Ordinary),  '@' },
            { (PlayerId.Player2, DiscType.Ordinary),  '#' },
            { (PlayerId.Player1, DiscType.Boring),    'B' }, 
            { (PlayerId.Player2, DiscType.Boring),    'b' },
            { (PlayerId.Player1, DiscType.Exploding), 'E'  },
            { (PlayerId.Player2, DiscType.Exploding), 'e'  },
            { (PlayerId.Player1, DiscType.Magnetic), 'M'  },
            { (PlayerId.Player2, DiscType.Magnetic), 'm'  },
            };

        public static char Get(PlayerId player, DiscType type)
        {
            return _glyphs[(player, type)];
        }


        private static readonly Dictionary<char, (PlayerId owner, DiscType type)> _rev =
        new()
        {
            // mirrored for re-writing the board from text
            ['@'] = (PlayerId.Player1, DiscType.Ordinary),
            ['#'] = (PlayerId.Player2, DiscType.Ordinary),
            ['B'] = (PlayerId.Player1, DiscType.Boring),
            ['b'] = (PlayerId.Player2, DiscType.Boring),
            ['E'] = (PlayerId.Player1, DiscType.Exploding),
            ['e'] = (PlayerId.Player2, DiscType.Exploding),
            ['M'] = (PlayerId.Player1, DiscType.Magnetic),
            ['m'] = (PlayerId.Player2, DiscType.Magnetic),
        };

        public static bool TryParse(char ch, out PlayerId owner, out DiscType type)
        {
            if (_rev.TryGetValue(ch, out var t)) { owner = t.owner; type = t.type; return true; }
            owner = default; type = default; return false;
        }
    }
}
