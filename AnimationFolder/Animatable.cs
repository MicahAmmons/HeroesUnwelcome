using Heroes_UnWelcomed.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public abstract class Animatable 
    {
        private AnimationController AnimContr {  get; set; }

        public Animatable(string animationName)
        {
            if (animationName == "EmptyCell") { AnimContr = null; return; }
            AnimContr = new AnimationController(animationName);
        }
        public virtual void UpdateAnimatable(GameTime g)
        {
            AnimContr?.Update(g);
        }
        public virtual void DrawAnimatable(SpriteBatch s)
        {
            AnimContr?.Draw(s);
        }
        public virtual void ReplaceAnimation(string newAnimName)
        {
            AnimContr = new AnimationController(newAnimName);
        }

    }
}
