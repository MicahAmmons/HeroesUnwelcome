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
        private Vector2 _leadPos;
        public PartyController(List<Hero> heroes)
        {
            _heroes = new List<Hero>(heroes);
            SetStartingPositions();
        }
        private void SetStartingPositions()
        {
            for (int i = 0;  i < _heroes.Count; i++)
            {
                _heroes[i].SetCurrentPosition(i);
            }
        }
        public void Update(GameTime gameTime)
        {
            foreach (var hero in _heroes)
            {
                hero.UpdateAnimatable(gameTime);
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
