using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public class AnimationBunch
    {
        private List<SingleAnimation> _animations = new List<SingleAnimation>();
        public AnimationBunch(List<SpecificAnimationData> animData)
        {
            foreach (var animation in animData)
            {
                _animations.Add(new SingleAnimation(animation));
            }
        }

        internal void Draw(SpriteBatch s)
        {
            foreach (var singleAnim in _animations)
            {
                int frameCount = singleAnim.GetFrameCount();
                if (singleAnim == null || frameCount < 1)
                    return;
                s.Draw(
                    texture: singleAnim.GetTexture(),
                    position: new Vector2(300, 200),
                    sourceRectangle: singleAnim.GetFrame(),
                    color: Color.White,
                    rotation: 0f,
                    origin: singleAnim.GetOrigin(),
                    scale: new Vector2(1f, 1f),
                    effects: singleAnim.GetSpriteEffect(),
                    layerDepth: 0f
                );

            }
        }

        internal void Update(GameTime g)
        {
            float delta = (float)g.ElapsedGameTime.TotalMilliseconds;
            foreach (var singleAnim in _animations)
            {
                if (singleAnim == null || singleAnim.GetFrameCount() < 1)
                    return;
                singleAnim.UpdateTimer(delta);
            }
        }
    }
}
