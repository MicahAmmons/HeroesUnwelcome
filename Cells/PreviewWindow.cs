using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Encounters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Heroes_UnWelcomed.Cells
{
    public class PreviewWindow : Animatable
    {
        private Rectangle DestinationRect = new Rectangle(0, 0, 200, 200);
        private EncounterData _encData = null;


        public PreviewWindow(string animationData = null) : base(animationData)
        {

        }
        public void UpdatePos(Vector2 pos)
        {
            CurrentPosition = pos;
        }
        public override void DrawAnimatable(SpriteBatch s)
        {
            AnimContr?.Draw(s, CurrentPosition);
        }
    }   
}
