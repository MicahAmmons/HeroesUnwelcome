using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Libraries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Heroes
{
    internal static class HeroManager
    {
        private static List<Hero> _heroes = new List<Hero>();
        public static List<Hero> AllHeroes => _heroes;
        public static void Initialize()
        {
           _heroes.Add(new Hero("Vampire"));
           _heroes.Add(new Hero("Goblin"));
        }

        internal static List<Hero> GetParty()
        {
            return new List<Hero>(_heroes);
        }
    }
}
