using Heroes_UnWelcomed.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;


namespace Heroes_UnWelcomed.AnimationFolder
{
    public class SingleAnimation
    {
        protected Texture2D SpriteSheet;
        protected int FrameCount;
        protected int DrawWidth;
        protected int DrawHeight;
        protected int FrameWidth;
        protected int FrameHeight;
        protected float FrameDurationMS;
        protected List<Rectangle> Frames = new List<Rectangle>();
        protected int CurrentFrameIndex = 0;
        protected float FrameTimer = 0f;
        protected Vector2 Origin;

        public SingleAnimation(SpecificAnimationData data)
        {
            SpriteSheet = AssetManager.GetTexture(data.SpriteSheetName);
           // DrawWidth = data.DrawWidth;
          //  DrawHeight = data.DrawHeight;
            FrameWidth = data.FrameWidth;
            FrameHeight = data.FrameHeight;
            FrameDurationMS = data.FrameDurationMs;
            Origin = new Vector2(FrameWidth / 2, FrameHeight);

            int framesPerRow = Math.Max(1, SpriteSheet.Width / data.FrameWidth);
            int startRowIndex = Math.Max(0, data.Row - 1);
            var frames = new List<Rectangle>(data.FrameCount);
            for (int i = 0; i < data.FrameCount; i++)
            {
                int col = i % framesPerRow;
                int rowOffset = i / framesPerRow;

                int x = col * data.FrameWidth;
                int y = (startRowIndex + rowOffset) * data.FrameHeight;

                if (y + data.FrameHeight > SpriteSheet.Height)
                    break;


                // main overlay frame
                frames.Add(new Rectangle(x, y, data.FrameWidth, data.FrameHeight));               
            }
            Frames.AddRange(FinalizeRectangleList(frames));
            FrameCount = Frames.Count;
        }
        public Rectangle GetFrame()
        {
            return Frames[CurrentFrameIndex];
        }
        private List<Rectangle> FinalizeRectangleList(List<Rectangle> frames)
        {
            //if (ReverseBuild)
            //{
            //    frames.Reverse();
            //}
            //if (EndCyclePause > 0)
            //{
            //    for (int i = 0; i < EndCyclePause; i++)
            //    {
            //        var dupedFrame = frames[frames.Count - 1];
            //        var frame = new Rectangle()
            //        {
            //            X = dupedFrame.X,
            //            Y = dupedFrame.Y,
            //            Width = dupedFrame.Width,
            //            Height = dupedFrame.Height
            //        };
            //        frames.Add(frame);
            //    }
            //}
            //if (PingPong)
            //{
            //    for (int i = frames.Count - 1; i >= 0; i--)
            //    {
            //        var currentFrame = frames[i];
            //        var newFrame = new Rectangle()
            //        {
            //            X = currentFrame.X,
            //            Y = currentFrame.Y,
            //            Width = currentFrame.Width,
            //            Height = currentFrame.Height
            //        };
            //        frames.Add(newFrame);
            //    }
            //}
            //if (StartCyclePause > 0)
            //{
            //    for (int i = 0; i < StartCyclePause; i++)
            //    {
            //        Frames.Add(new Rectangle());
            //    }
            //}
            return frames;
        }
        public int GetFrameCount()
        {
            return FrameCount;
        }
        public float GetFrameDurationMs()
        {
            return FrameDurationMS;
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

        internal Texture2D GetTexture()
        {
            return SpriteSheet;
        }

        internal SpriteEffects GetSpriteEffect()
        {
            return SpriteEffects.None;
        }

        internal Vector2 GetOrigin()
        {
            return Origin;
        }
    }
}
