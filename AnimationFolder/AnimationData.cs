using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public class AnimationData :
    Dictionary<string, Dictionary<AnimationType, List<SpecificAnimationData>>>
    {
    }
    public class SpecificAnimationData
    {
        public string SpriteSheetName { get; set; }
        public int FrameCount { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public float FrameDurationMs { get; set; }
        public int Row { get; set; }
        public Direction DefaultDirection { get; set; }
    }
}
