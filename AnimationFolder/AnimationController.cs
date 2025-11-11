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
        private Dictionary<AnimationType, AnimationBunch> _animations = new Dictionary<AnimationType, AnimationBunch>();

        public AnimationController(string animationName)
        {
            Dictionary<AnimationType, List<SpecificAnimationData>> data = AnimationLibrary.GetAnimationData(animationName);

            foreach (var kvp in data)
            {
                AnimationType type = kvp.Key;
                List<SpecificAnimationData> animData = kvp.Value;
                _animations[type] = new AnimationBunch(animData);
            }
        }

        public void Update(GameTime g)
        {
            UpdateBunch(g);
        }
        private void UpdateBunch(GameTime g)
        {
            _animations[_currentAnimation]?.Update(g);
        }
        public void Draw(SpriteBatch s, Vector2 position)
        {
            _animations[_currentAnimation]?.Draw(s, position);
        }
        protected virtual void SetCurrentAnimation(AnimationType type)
        {
            _currentAnimation = type;
        }
    }
}
