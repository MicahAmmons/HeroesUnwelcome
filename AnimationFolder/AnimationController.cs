using Heroes_UnWelcomed.Libraries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public class AnimationController
    {
        private AnimationType _currentAnimationType = AnimationType.Idle;
        private Dictionary<AnimationType, AnimationBunch> _animations = new Dictionary<AnimationType, AnimationBunch>();
        private bool _isPlaying = true;
        public AnimationController(string animationName)
        {
            if (animationName is null)
            {
                _isPlaying = false;
                return;
            }
            //placeholder for hallway constructor 
            if(animationName is "Hallway")
            {
                Dictionary<AnimationType, AnimationData> hallwayData = new Dictionary<AnimationType, AnimationData>();
                var specific = new SpecificAnimationData()
                {
                    TotalFrames = 2,
                    Frames = new List<string>()
                    {
                        "Hallway1",
                        "Hallway2"
                    },
                    DefaultDirection = Direction.Right,
                    DrawPos = DrawPosition.Hallway,
                    
                };

                hallwayData[AnimationType.Idle] = new AnimationData()
                {
                    AllAnimationsPerType = new List<SpecificAnimationData>()
                    {
                        specific
                    }
                };
                foreach (var kvp in hallwayData)
                {
                    AnimationType type = kvp.Key;
                    AnimationData animData = kvp.Value;
                    _animations[type] = new AnimationBunch(animData);
                }
                return;

            }
            Dictionary<AnimationType, AnimationData> data = AnimationLibrary.GetAnimationData(animationName);

            foreach (var kvp in data)
            {
                AnimationType type = kvp.Key;
                AnimationData animData = kvp.Value;
                _animations[type] = new AnimationBunch(animData);
            }
        }

        public void Update(float delta)
        {
            UpdateBunch(delta);
        }
        private void UpdateBunch(float delta)
        {
            if (!_isPlaying) return;
            _animations[_currentAnimationType]?.Update(delta);
            
        }
        public void Draw(SpriteBatch s, Vector2 position, bool isPreview = false)
        {
            if (!_isPlaying) return;
            _animations[_currentAnimationType]?.Draw(s, position, isPreview);
        }
        protected virtual void SetCurrentAnimation(AnimationType type)
        {
            _currentAnimationType = type;
        }
    }
}
