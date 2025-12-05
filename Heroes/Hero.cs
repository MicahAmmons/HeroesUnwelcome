using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Charges;
using System;

namespace Heroes_UnWelcomed.Heroes
{
    public class Hero : Moving
    {
        public int Attack = 5;
        public int Agility = 5;
        public int Intelligence = 5;
        public string Name;
        private Charge CurrentCharge = null;
        private int _chargeIndex = 0;
        public Hero(string name) : base(name)
        {
            Name = name;
        }
        public bool HasCurrentCharge => CurrentCharge != null;
        public bool ChargeIsFinished => CurrentCharge != null && CurrentCharge.IsFinished;
    }
}
