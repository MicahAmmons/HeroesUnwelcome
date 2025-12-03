using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Charges;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.ScreenReso;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Heroes_UnWelcomed.Cells
{
    public class Cell : Animatable
    {
        private EncounterBunch _encounterBunch = null;
        public bool Full => _encounterBunch != null;
        public EncounterBunch Encounter
        {
            get => _encounterBunch;
            set
            {
                if (_encounterBunch == value) return;
                bool wasEmpty = _encounterBunch == null;
                _encounterBunch = value;
                if (wasEmpty && _encounterBunch != null)
                    CellManager.MarkFull(this);
            }
        }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public const int Width = 1920;
        public const int Height = 1080;
        public Rectangle DestinationRect { get; private set; }
        public Vector2 Origin { get; set; }

        private AnimationController _encHoverAnimation = null;
        private int _hoverAnimTimer = 0;


        public Cell(int x, int y, string animationName = "EmptyCell") : base(animationName)
        {
            GridX = x;
            GridY = y;
            Initialize(x,y);
            Origin = Vector2.Zero;
        }
        public override void DrawAnimatable(SpriteBatch s)
        {
            if (_encHoverAnimation != null)
            {
                _encHoverAnimation.Draw(s, CurrentPosition, true);
            }
            else if (_encounterBunch != null)
            {
                _encounterBunch.Draw(s);
            }
            else
                base.DrawAnimatable(s);
        }
        public override void UpdateAnimatable(float delta)
        {
            if (_encHoverAnimation != null)
            {
                _encHoverAnimation.Update(delta);
                CheckIfShouldDelete();
            }
            else if (_encounterBunch != null)
            {
                _encounterBunch.UpdateEncounters(delta);
            }
            else
                base.UpdateAnimatable(delta);
        }

        private void CheckIfShouldDelete()
        {
            _hoverAnimTimer++;
            if (_hoverAnimTimer >= 1)
            {
                _encHoverAnimation = null;
                _hoverAnimTimer = 0;
            }
        }

        private void Initialize(int x, int y)
        {
            CurrentPosition = new Vector2(x * Width, y * Height);
            DestinationRect = new Rectangle((int)CurrentPosition.X, (int)CurrentPosition.Y, Width, Height);
        }
        public void AddEncounter(string encounter)
        {
            _encounterBunch = new EncounterBunch(EncounterLibrary.GetEncounterData(encounter), encounter, DestinationRect);
        }
        internal void DrawOutLine(SpriteBatch s)
        {
            var pixel = AssetManager.GetTexture("WhitePixel");
            if (pixel == null) return;

            int t = 4;
            var c = Color.Magenta;
            var r = DestinationRect;

            // helper local clamp so width/height never overflow
            int ClampX(int x) => Math.Clamp(x, r.X, r.X + r.Width);
            int ClampY(int y) => Math.Clamp(y, r.Y, r.Y + r.Height);
            int ClampW(int w) => Math.Clamp(w, 0, r.Width);
            int ClampH(int h) => Math.Clamp(h, 0, r.Height);

            // BORDER --------------------------------------------------------------------

            s.Draw(pixel, new Rectangle(ClampX(r.X), ClampY(r.Y), ClampW(r.Width), t), c); // Top
            s.Draw(pixel, new Rectangle(ClampX(r.X), ClampY(r.Y + r.Height - t), ClampW(r.Width), t), c); // Bottom
            s.Draw(pixel, new Rectangle(ClampX(r.X), ClampY(r.Y), t, ClampH(r.Height)), c); // Left
            s.Draw(pixel, new Rectangle(ClampX(r.X + r.Width - t), ClampY(r.Y), t, ClampH(r.Height)), c); // Right

            // CENTER VERTICAL LINE ------------------------------------------------------

            int centerX = ClampX(r.X + r.Width / 2);
            s.Draw(pixel, new Rectangle(centerX, ClampY(r.Y), t, ClampH(r.Height)), Color.Cyan);

            // 80% HORIZONTAL ------------------------------------------------------------

            int y80 = ClampY(r.Y + (int)(r.Height * .8f));
            s.Draw(pixel, new Rectangle(ClampX(r.X), y80, ClampW(r.Width), t), Color.Yellow);

            // 75% VERTICAL --------------------------------------------------------------

            int x75 = ClampX(r.X + (int)(r.Width * 0.75f));
            s.Draw(pixel, new Rectangle(x75, ClampY(r.Y), t, ClampH(r.Height)), Color.LimeGreen);

            // 16% HORIZONTAL ------------------------------------------------------------

            int y16 = ClampY(r.Y + (int)(r.Height * 0.16f));
            s.Draw(pixel, new Rectangle(ClampX(r.X), y16, ClampW(r.Width), t), Color.Orange);

        }
        internal void UpdateEncounterHover(string playerChosenEnc)
        {
            _hoverAnimTimer = 0;
            if (_encHoverAnimation == null)
            {
                _encHoverAnimation = new AnimationController(playerChosenEnc);
            }
        }

        internal List<ChargeData> FetchCharges()
        {
            return _encounterBunch.EncCharges;
        }
    }

}
