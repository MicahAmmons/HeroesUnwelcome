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
        private static float _timeMod;

        private const float _pauseSpeed = 0f;
        private const float _playSpeed = .75f;
        private const float _2xSpeed = 1.5f;
        private const float _3xSpeed = 4.5f;


        public static void Initialize()
        {
            _timeMod = _playSpeed;
        }
        public static float Update(GameTime g)
        {
            _delta = (float)g.ElapsedGameTime.TotalMilliseconds * _timeMod;

            return _delta;
        }
        public static void PlayerChoseSpeed(string speed)
        {
            float timeMod = 0f;
            switch (speed)
            {
                case "Play":
                    timeMod = _playSpeed;
                    break;
                case "Pause":
                    timeMod = _pauseSpeed;
                    break;
                case "2xSpeed":
                    timeMod = _2xSpeed;
                    break;
                case "3xSpeed":
                    timeMod = _3xSpeed;
                    break;
                case null:
                    return;
            }
            if (timeMod != _timeMod)
            {
                _timeMod = timeMod;
            }
        }
    }
}
