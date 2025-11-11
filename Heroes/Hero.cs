using Heroes_UnWelcomed.AnimationFolder;
using System;

namespace Heroes_UnWelcomed.Heroes
{
    public class Hero : Moving
    {
        public int PosInParty;
        public Hero(HeroData data) : base(data.Animation)
        {

        }

        internal void SetCurrentPosition(int i)
        {
            PosInParty = i;
        }
    }
}
