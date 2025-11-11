using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.ScreenReso;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Cells
{
    public static class CellManager
    {
        private static readonly List<Cell> _all = new();
        private static readonly List<Cell> _empty = new();
        private static readonly List<Cell> _full = new();
        public static IReadOnlyCollection<Cell> All => _all;
        public static IReadOnlyCollection<Cell> Empty => _empty;
        public static IReadOnlyCollection<Cell> Full => _full;

        public static event Action<Rectangle> TheWorldRectChanged;

        private static List<Vector2> DungeonPath = new List<Vector2>();
        private static PartyController _partyContr = null;
        public static void Initialize()
        {
            AddCell(new Cell(0, 0));
            AddCell(new Cell(1, 0));
            AddCell(new Cell(-1, 0));
            AddCell(new Cell(0, 1));
            //AddCell(new Cell(0, -1));
            MarkFull(_all.First());
            _partyContr = new PartyController(HeroManager.GetParty());

        }
        public static void AddCell(Cell c)
        {

            _all.Add(c);
            AdjustWorldRectangle();
            _empty.Add(c);
        }
        private static Rectangle _contentRect = Rectangle.Empty; // tight bounds around cells (no padding)
        public static Rectangle ContentRectangle => _contentRect;
        public static Rectangle WorldRectangle { get; private set; } = Rectangle.Empty; // padded rect you expose
        private static void AdjustWorldRectangle( int totalPadX = 600)
        {
            if (_all.Count == 0)
            {
                _contentRect = new Rectangle(0, 0, 0, 0);
                WorldRectangle = _contentRect;
                TheWorldRectChanged?.Invoke(WorldRectangle);
                return;
            }

            // 1) Tight pixel-bounds around all cells (min..max inclusive)
            int minGX = _all.Min(c => c.GridX);
            int maxGX = _all.Max(c => c.GridX);
            int minGY = _all.Min(c => c.GridY);
            int maxGY = _all.Max(c => c.GridY);

            int cellCols = (maxGX - minGX + 1);
            int cellRows = (maxGY - minGY + 1);

            // Top-left of the min grid cell in pixels:
            int contentX = minGX * Cell.Width;
            int contentY = minGY * Cell.Height;
            int contentW = cellCols * Cell.Width;
            int contentH = cellRows * Cell.Height;

            _contentRect = new Rectangle(contentX, contentY, contentW, contentH);

            // 2) Symmetric padding (X given; Y proportional to viewport aspect)
            int totalPadY = (int)Math.Round(totalPadX * (ScreenSize.Height / (float)ScreenSize.Width)); // keeps similar screen fraction

            int halfPadX = totalPadX / 2;
            int halfPadY = totalPadY / 2;

            // 3) Final "world" = content + symmetric padding
            WorldRectangle = new Rectangle(
                _contentRect.X - halfPadX,
                _contentRect.Y - halfPadY,
                _contentRect.Width + totalPadX,
                _contentRect.Height + totalPadY
            );

            TheWorldRectChanged?.Invoke(WorldRectangle);
        }
        internal static void MarkFull(Cell cell)
        {
            _empty.Remove(cell);
            _full.Add(cell); 
            UpdateFullDungeonPath();
        }
        public static void DrawCells(SpriteBatch s)
        {
            foreach (var cell in _full)
            {
                cell.DrawStaticCell(s);
                cell.DrawAnimatable(s);
            }
            foreach (var cell in _empty)
            {
                cell.DrawEmptyCell(s);
            }
        }
        internal static void DrawCellOutLine(SpriteBatch spriteBatch)
        {
            foreach (var cell in _all)
            {
                cell.DrawOutLine(spriteBatch);
            }
        }
        private static void UpdateFullDungeonPath()
        {
            List<Vector2> final = new List<Vector2>();

            for (int i = 0; i < _full.Count; i++)
            {
                final.Add(_full[i].PathStart);
                final.Add(_full[i].PathEnd);
            }

            DungeonPath = new List<Vector2>(final);
        }
        internal static void Update(GameTime gameTime)
        {
            _partyContr?.Update(gameTime);
        }
        internal static void DrawParties(SpriteBatch s)
        {
            _partyContr?.DrawHeroes(s);
        }
    }
}
