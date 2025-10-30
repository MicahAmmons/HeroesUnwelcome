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
        private static readonly HashSet<Cell> _empty = new();
        private static readonly HashSet<Cell> _full = new();
        public static IReadOnlyCollection<Cell> All => _all;
        public static IReadOnlyCollection<Cell> Empty => _empty;
        public static IReadOnlyCollection<Cell> Full => _full;

        public static void Initialize()
        {
            AddCell(new Cell(0, 0));
            AddCell(new Cell(1, 0));
            AddCell(new Cell(-1, 0));
            AddCell(new Cell(0, 1));
            AddCell(new Cell(0, -1));

        }
        public static void AddCell(Cell c)
        {
            if (_all.Contains(c)) return; 
            _all.Add(c);
            _empty.Add(c);
        }
        internal static void MarkFull(Cell cell)
        {
            _empty.Remove(cell);
            _full.Add(cell); 
        }
        public static void DrawCells(SpriteBatch s)
        {
            foreach (var cell in _full)
            {
                cell.DrawAnimatable(s);
            }
            foreach (var cell in _empty)
            {
                cell.DrawAnimatable(s);
            }
        }

    }
}
