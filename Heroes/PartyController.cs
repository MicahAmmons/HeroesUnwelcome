using Heroes_UnWelcomed.AnimationFolder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Heroes
{
    public class PartyController
    {
        private List<Hero> _heroes;
        public PartyController(List<Hero> heroes)
        {
            _heroes = new List<Hero>(heroes);
        }
        public void Update(float delta)
        {
            foreach (var hero in _heroes)
            {

                hero.UpdateAnimatable(delta);
            }
        }
        public void DrawHeroes(SpriteBatch s)
        {
            foreach (var hero in _heroes)
            {
                hero.DrawAnimatable(s);
            }
        }
    }
}
