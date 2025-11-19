using Heroes_UnWelcomed.Charges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Encounters
{
    public class EncounterData
    {
        public EncounterType Category;
        public int AttackPower { get; set; }
        public List<ChargeData> OrderOfCharges { get; set; }
    }
}
