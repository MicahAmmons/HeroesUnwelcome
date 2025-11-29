using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Cells;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;


namespace Heroes_UnWelcomed.AnimationFolder
{
    public class SingleAnimation
    {

        protected List<Texture2D> Frames = new List<Texture2D>();
        protected int FrameCount;
        protected float FrameDurationMS;
        protected int CurrentFrameIndex = 0;
        protected float FrameTimer = 0f;
        protected Vector2 Origin = Vector2.Zero;
        protected Vector2 DrawPoint = Vector2.Zero;
        protected DrawPosition DrawPos;

        public SingleAnimation(SpecificAnimationData data)
        {
            foreach (var key in data.Frames)
            {
                Frames.Add(AssetManager.GetTexture(key));
            }
            FrameDurationMS = 300;
            DrawPos = data.DrawPos;
            FrameCount = data.TotalFrames;

        }
        public int GetFrameCount()
        {
            return FrameCount;
        }
        internal void UpdateTimer(float delta)
        {
            FrameTimer += delta;
            if (FrameTimer >= FrameDurationMS)
            {
                UpdateCurrentFrame();
                FrameTimer -= FrameDurationMS;
            }

        }
        private void UpdateCurrentFrame()
        {
            var index = CurrentFrameIndex;
            index += 1;
            if (index < 0 || index >= Frames.Count)
            {
                CurrentFrameIndex = 0;
                return;
            }
            CurrentFrameIndex = index;
        }
        internal SpriteEffects GetSpriteEffect()
        {
            return SpriteEffects.None;
        }

        internal Vector2 GetOrigin()
        {
            if (Origin != Vector2.Zero)
                return Origin;

            switch (DrawPos)
            {
                case DrawPosition.Cell:
                    Origin = new Vector2(0, 0);
                    break;

                case DrawPosition.Hallway:
                    Origin = new Vector2(0, 0);
                    break;
                case DrawPosition.Combatant:
                    Origin = new Vector2(Frames[0].Width / 2, Frames[0].Height);
                    break;
            }
            return Origin;
        }

        internal Texture2D GetTexture()
        {
            return Frames[CurrentFrameIndex];
        }
        internal Vector2 GetDrawFromVector(Vector2 pos)
        {
            if (DrawPoint != Vector2.Zero)
                return DrawPoint;
 
            const float floor = Cell.Height * .80f;
            Vector2 basePos = pos;
            switch (DrawPos)
            {
                case DrawPosition.Cell:
                    DrawPoint = pos;
                    break;

                case DrawPosition.Hallway:
                    DrawPoint = new Vector2(pos.X, (Cell.Height *.38f) + pos.Y);
                    break;
                case DrawPosition.Combatant:
                    DrawPoint = new Vector2((Cell.Width * .875f) + pos.X, floor + pos.Y);
                    break;
            }
            return DrawPoint;
        }

        internal int GetWidth()
        {
            if (DrawPos == DrawPosition.Hallway)
            {
                return 960;
            }
            return Math.Clamp(Frames[CurrentFrameIndex].Width, 0, Cell.Width);
        }

        internal int GetHeight()
        {
            if (DrawPos == DrawPosition.Hallway)
            {
                return 455;
            }
            return Math.Clamp(Frames[CurrentFrameIndex].Height, 0, Cell.Height);
        }

        internal Color GetColor()
        {
            if (DrawPos == DrawPosition.Hallway)
            {
                return Color.DarkBlue * .5f;
            }
            return Color.White; 
        }
    }
}
