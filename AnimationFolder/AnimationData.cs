using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public class AnimationData
    {
        public List<SpecificAnimationData> AllAnimationsPerType { get; set; } = new List<SpecificAnimationData>();

    }
    public class SpecificAnimationData
    {
        public int TotalFrames { get; set; }
        public List<string> Frames { get; set; } = new List<string>();
        public Direction DefaultDirection { get; set; }
        public DrawPosition DrawPos { get; set; }
    }
}
