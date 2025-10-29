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
    internal class AnimationController
    {
        private AnimationType _currentAnimation = AnimationType.Idle;
        private Dictionary<AnimationType, List<SingleAnimation>> _animations = new Dictionary<AnimationType, List<SingleAnimation>>();

        public AnimationController(string animationName)
        {
            Dictionary<AnimationType, List<SpecificAnimationData>> data = AnimationLibrary.GetAnimationData(animationName);
            foreach (var kvp in data)
            {
                AnimationType type = kvp.Key;
                List<SpecificAnimationData> animData = kvp.Value;
                _animations[type] = new List<SingleAnimation>();
                foreach (var animation in animData)
                {
                    _animations[type].Add(new SingleAnimation(animation));
                }
            }
        }

        public void Update(GameTime g)
        {

        }
        public void Draw(SpriteBatch s)
        {
            foreach (var anim in _animations[_currentAnimation])
            {
                


            }
        }
        protected virtual void SetCurrentAnimation(AnimationType type)
        {
            _currentAnimation = type;
        }
    }
}
