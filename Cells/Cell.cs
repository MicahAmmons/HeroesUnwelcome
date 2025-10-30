using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Encounters;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Cells
{
    public class Cell : Animatable
    {
        private Encounter _encounter;
        public Encounter Encounter
        {
            get => _encounter;
            set
            {
                if (_encounter == value) return;
                bool wasEmpty = _encounter == null;
                _encounter = value;
                if (wasEmpty && _encounter != null)
                    CellManager.MarkFull(this);
            }
        }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public bool Full => _encounter != null;

        public Cell(int x, int y, string animationName = "EmptyCell"): base( animationName)
        {
            GridX = x;
            GridY = y;
        }
        public void AddEncounter(string encounter)
        {
            _encounter = new Encounter();
            ReplaceAnimation(encounter);
        }
    }
}
