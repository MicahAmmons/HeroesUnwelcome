using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.ScreenReso;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Heroes_UnWelcomed.Cells
{
    public class Cell : Animatable
    {
        private Encounter _encounter;
        public Encounter Encounter
        {
            get => _encounter;
            set
            {
                if (_encounter == value) return;
                bool wasEmpty = _encounter == null;
                _encounter = value;
                if (wasEmpty && _encounter != null)
                    CellManager.MarkFull(this);
            }
        }
        public int GridX { get; set; }
        public int GridY { get; set; }
        private Vector2 Position { get; set; }
        public const int Width = 3840;
        public const int Height = 2160;
        public Rectangle DestinationRect { get; private set; }
        public Vector2 Origin { get; set; }
        public bool Full => _encounter != null;
        public string EmptyText = "EmptyCell";


        public Cell(int x, int y, string animationName = "EmptyCell") : base(animationName)
        {
            GridX = x;
            GridY = y;
            Position = new Vector2(x * Width, y * Height);
            DestinationRect = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
            Origin = Vector2.Zero;
        }

        public void AddEncounter(string encounter)
        {
            _encounter = new Encounter();
            ReplaceAnimation(encounter);
        }

        internal void DrawEmptyCell(SpriteBatch s)
        {
            var tex = AssetManager.GetTexture(EmptyText);
            s.Draw(
                texture: tex,
                destinationRectangle: DestinationRect,
                sourceRectangle: null,
                color: Color.White
            );




        }


        internal void SetPosition(Vector2 pos)
        {
            Position = pos;
        }

        internal void DrawOutLine(SpriteBatch s)
        {
            var pixel = AssetManager.GetTexture("WhitePixel");
            if (pixel == null) return;

            int t = 4;
            var c = Color.Magenta;
            var r = DestinationRect;

            // Top
            s.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), c);
            // Bottom
            s.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - t, r.Width, t), c);
            // Left
            s.Draw(pixel, new Rectangle(r.X, r.Y, t, r.Height), c);
            // Right
            s.Draw(pixel, new Rectangle(r.X + r.Width - t, r.Y, t, r.Height), c);
        }
    }
}
