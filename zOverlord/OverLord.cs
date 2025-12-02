using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Overlord
{
    public static class OverLord
    {
        private static float _delta;
        public static float Delta => _delta;
        private static float _timeMod = 2f;

        public static float Update(GameTime g)
        {
            _delta = (float)g.ElapsedGameTime.TotalMilliseconds * _timeMod;

            return _delta;
        }
    }
}
