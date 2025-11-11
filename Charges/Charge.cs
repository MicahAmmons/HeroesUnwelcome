using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Charges
{
    public class Charge
    {
        public ChargeType Type { get; set; }

    }
    public class CombatCharge:Charge
    {

    }
    public class TravelCharge : Charge
    {
        public Vector2 Begin;
        public Vector2 End;

    }
    public class ExitCharge : Charge
    {

    }
}


