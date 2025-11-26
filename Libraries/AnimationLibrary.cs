using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.Libraries
{
    public static class AnimationLibrary
    {
        private static Dictionary<string, Dictionary<AnimationType, AnimationData>> _data = new Dictionary<string, Dictionary<AnimationType, AnimationData>>();

        internal static void Initialize()
        {
        }
        public static Dictionary<AnimationType, AnimationData> GetAnimationData(string name)
        {
            return _data[name];
        }
        internal static void AddAnimationFrame(string encounter, AnimationType animName, List<string> key, DrawPosition drawPos)
        {
            if (!_data.ContainsKey(encounter))
            {
                _data[encounter] = new Dictionary<AnimationType, AnimationData>();
            }
            if (!_data[encounter].ContainsKey(animName))
            {
                _data[encounter].Add(animName, new AnimationData());
            }

            _data[encounter][animName].AllAnimationsPerType.Add(new SpecificAnimationData()
            {
                Frames = new List<string>(key),
                TotalFrames = key.Count,
                DefaultDirection = Direction.Left,
                DrawPos = drawPos
            });

        }
    }
}   
