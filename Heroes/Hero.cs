using Heroes_UnWelcomed.AnimationFolder;
using System;

namespace Heroes_UnWelcomed.Heroes
{
    public class Hero : Moving
    {
        public int PosInParty;
        public int Attack = 5;
        public int Agility = 5;
        public int Intelligence = 5;
        public Hero(string name) : base(name)
        {

        }

        internal void SetCurrentPosition(int i)
        {
            PosInParty = i;
        }
    }
}
