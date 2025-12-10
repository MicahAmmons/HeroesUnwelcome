using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Cells;
using Heroes_UnWelcomed.Charges;
using Heroes_UnWelcomed.Encounters;
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
        internal void SetStartingPositions()
        {
            float doorX = Encounter.DoorX;
            float floor = Encounter.GroundFloor;
            Rectangle cellRect = new Rectangle(0, 0, Cell.Width, Cell.Height);
            int count = _heroes.Count;
            float startX = cellRect.Width * doorX;
            float endX = cellRect.Width;
            float y = cellRect.Height * floor;

            float spacing = 0f;
            if (count > 1)
                spacing = (endX - startX) / (count - 1);

            for (int i = 0; i < count; i++)
            {
                var hero = _heroes[i];
                int reversedIndex = (count - 1) - i;

                float x = startX + (spacing * reversedIndex);

                hero.SetCurrentPosition(new Vector2(x, y));
            }
        }

    }
}
