using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.Utilities;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.Libraries
{
    public static class AnimationLibrary
    {
        private static Dictionary<string, Dictionary<AnimationType, List<SpecificAnimationData>>> _animationLibrary = new Dictionary<string, Dictionary<AnimationType, List<SpecificAnimationData>>>();

        internal static void Initialize()
        {
            _animationLibrary = JsonLoader.LoadAnimationData();
        }
        public static Dictionary<AnimationType, List<SpecificAnimationData>> GetAnimationData(string name)
        {
            return _animationLibrary[name];
        }
    }
}   
