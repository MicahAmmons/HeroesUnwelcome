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
        private static List<Moving> _heroes = new List<Moving>();
        public static List<Moving> AllHeroes => _heroes;
        public static void Initialize()
        {
            _heroes.Add(new Hero(HeroLibrary.GetHeroData("Ranger")));
            _heroes.Add(new Hero(HeroLibrary.GetHeroData("Ranger")));
            _heroes.Add(new Hero(HeroLibrary.GetHeroData("Ranger")));
        }
        public static void Update(GameTime gameTime)
        {
            foreach (var hero in _heroes)
            {
                hero.UpdateAnimatable(gameTime);
            }
        }
        public static void DrawHeroes(SpriteBatch s)
        {
            foreach (var hero in _heroes)
            {
                hero.DrawAnimatable(s);
            }
        }

    }
}
