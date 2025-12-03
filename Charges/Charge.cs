using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Charges
{
    public abstract class Charge
    {
        private bool _sFinished = false;
        public bool IsFinished => _sFinished;
    }
}
