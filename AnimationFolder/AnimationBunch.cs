using Heroes_UnWelcomed.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public class AnimationBunch
    {
        private List<SingleAnimation> _animations = new List<SingleAnimation>();
        public AnimationBunch(AnimationData animData)
        {
            foreach (var animation in animData.AllAnimationsPerType)
            {
                _animations.Add(new SingleAnimation(animation));
            }
        }

        internal void Draw(SpriteBatch s, Vector2 pos, bool preview = false)
        {
            foreach (var singleAnim in _animations)
            {
                if (singleAnim == null || singleAnim.GetFrameCount() < 1)
                    continue;

                Texture2D text = singleAnim.GetTexture();
                Vector2 drawFrom = singleAnim.GetDrawFromVector(pos);
                int width = singleAnim.GetWidth();
                int height = singleAnim.GetHeight();
                Vector2 origin = singleAnim.GetOrigin();
                SpriteEffects effects = singleAnim.GetSpriteEffect();
                Color col = singleAnim.GetColor();
                Rectangle dest = new Rectangle((int)drawFrom.X, (int)drawFrom.Y, width, height);

                s.Draw(
                    texture: text,
                    destinationRectangle: dest,
                    sourceRectangle: null,
                    color: col,
                    rotation: 0f,
                    origin: origin,          // <--- key change
                    effects: effects,              // flipping still fine
                    layerDepth: 0f
                );
                Rectangle outlineRect = new Rectangle(
    dest.X - (int)origin.X,
    dest.Y - (int)origin.Y,
    dest.Width,
    dest.Height
);
                DrawOutline(s, outlineRect, origin);
            }
        }

        internal void DrawOutline(SpriteBatch s, Rectangle r, Vector2 origin)
        {
            var pixel = AssetManager.GetTexture("WhitePixel");
            if (pixel == null) return;

            int t = 3;
            Color c = Color.HotPink;

            s.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), c);
            s.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - t, r.Width, t), c);
            s.Draw(pixel, new Rectangle(r.X, r.Y, t, r.Height), c);
            s.Draw(pixel, new Rectangle(r.X + r.Width - t, r.Y, t, r.Height), c);
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
