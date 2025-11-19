using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Charges
{
    public class ChargeData
    {
        public ChargeType Type { get; set; }
        public Position Begin { get; set; } = Position.CurrentPos;
        public Position End { get; set; } = Position.CurrentPos;
    }
}
