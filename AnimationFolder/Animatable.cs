using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Heroes;
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
        public AnimationController AnimContr {  get; set; }
        public Vector2 CurrentPosition { get; set; }

        public Animatable(string animationName)
        {
            if (animationName is "Null" or null) { AnimContr = null; return; }
            AnimContr = new AnimationController(animationName);
        }
        public virtual void UpdateAnimatable(float delta)
        {
            AnimContr?.Update(delta);

        }
        public virtual void DrawAnimatable(SpriteBatch s)
        {
            AnimContr?.Draw(s, CurrentPosition);
        }
        public virtual void ReplaceAnimation(string newAnimName)
        {

            AnimContr = new AnimationController(newAnimName);
        }

        public virtual void SelfDeleteAnimContr()
        {
            AnimContr = null;
        }

    }
}
