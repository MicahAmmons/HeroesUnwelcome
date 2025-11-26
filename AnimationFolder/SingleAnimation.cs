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
        protected Vector2 Origin;
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
            Vector2 final = Vector2.One;
            switch (DrawPos)
            {
                case DrawPosition.Cell:
                    final = new Vector2(0, 0);
                    break;

                case DrawPosition.Hallway:
                    final = new Vector2(0, 0);
                    break;
                case DrawPosition.Combatant:
                    final = new Vector2(Frames[0].Width / 2, Frames[0].Height);
                    break;
            }
            return final;
        }

        internal Texture2D GetTexture()
        {
            return Frames[CurrentFrameIndex];
        }
        internal Vector2 GetDrawFromVector(Vector2 pos)
        {

            int cellWidth = Cell.Width;
            const float floor = Cell.Height * .80f;
            Vector2 basePos = pos;
            Vector2 drawPoint = Vector2.Zero;
            switch (DrawPos)
            {
                case DrawPosition.Cell:
                    drawPoint = pos;
                    break;

                case DrawPosition.Hallway:
                    drawPoint = pos;
                    break;
                case DrawPosition.Combatant:
                    drawPoint = new Vector2(cellWidth * .875f, floor);
                    break;
            }
            return drawPoint;
        }

        internal int GetWidth()
        {
            return Math.Clamp(Frames[CurrentFrameIndex].Width, 0, Cell.Width);
        }

        internal int GetHeight()
        {
            return Math.Clamp(Frames[CurrentFrameIndex].Height, 0, Cell.Height);
        }
    }
}
