using Heroes_UnWelcomed.Charges;
using Heroes_UnWelcomed.DebugBugger;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.InputTracker;
using Heroes_UnWelcomed.ScreenReso;
using Heroes_UnWelcomed.UI;
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
        public static event Action<List<ChargeData>> EncounterAdded;

        private static Cell _currentlyHoveredCell = null;
        public static Cell CurrentHoveredCell => _currentlyHoveredCell;
        private static PreviewWindow _previewWindow;


        public static void Initialize()
        {
            Cell firstCell = new Cell(0, 0);
            AddCell(firstCell);
            _currentlyHoveredCell = firstCell;
            _playerChosenEnc = "SpawnIn";
            ConfirmNewEncounter();
            _playerChosenEnc = null;
            //AddCell(new Cell(1, 0));
            //AddCell(new Cell(-1, 0));
            //AddCell(new Cell(0, 1));
           // AddCell(new Cell(0, -1));
            //MarkFull(_all.First());
            _previewWindow = new PreviewWindow();
        }
        public static void AddCell(Cell c)
        {

            _all.Add(c);
            AdjustWorldRectangle();
            _empty.Add(c);
        }
        private static Rectangle _contentRect = Rectangle.Empty; 
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
            //UpdateFullDungeonPath();
        }
        public static void DrawCells(SpriteBatch s)
        {
            foreach (var cell in _all)
            {
                cell.DrawAnimatable(s);
            }
        }
        internal static void DrawCellOutLine(SpriteBatch spriteBatch)
        {
            foreach (var cell in _all)
            {
                cell.DrawOutLine(spriteBatch);
            }
        }
        public static void UpdateAllCells(float delta)
        {
            foreach (var cell in _all)
            {
                cell.UpdateAnimatable(delta);
            }
        }
        internal static void Update(float delta)
        {
  

            UpdateAllCells(delta);

            ShowPlayerChosenEncounterToSpawn();

            UpdatePreviewWindow();

            UpdatePlayerCurrentCellHover();
        }
        private static void UpdatePreviewWindow()
        {
            if (!_drawPreviewWindow) return;
            if (CurrentHoveredCell != null || _playerChosenEnc == null) return;
            //_previewWindow.UpdatePos(TapTap.Position);
        }
        private static void UpdatePlayerCurrentCellHover()
        {
            foreach (var cell in _all)
            {
                Vector2 pos = TapTap.WorldPositon;
                if (cell.DestinationRect.Contains(pos))
                {
                    _currentlyHoveredCell = cell;
                    Debug.UpdateHoveredCell(cell.GridX, cell.GridY);
                    if (_currentlyHoveredCell.Encounter != null)
                    {
                        foreach (var enc in _currentlyHoveredCell.Encounter.Encounters)
                        {
                            Debug.UpdateCurrentHoveredEncounter(enc.Name);
                            break;
                        }
                        Debug.UpdateCurrentHoveredEncounter(null);
                    }
                    else
                        Debug.UpdateCurrentHoveredEncounter(null);
                        return;
                }
            }
                _currentlyHoveredCell = null;
                Debug.UpdateHoveredCell();
            }
        private static string _playerChosenEnc = null;
        private static void ShowPlayerChosenEncounterToSpawn()
        {
            ToggleEncounterPreviewDrawn();
        }
        private static void ToggleEncounterPreviewDrawn()
        {
            if (_playerChosenEnc == null)
            {
                _drawPreviewWindow = false;
                return;
            }
            var cell = _currentlyHoveredCell;

            //Draw preview window
            if (cell == null)
            {
                _drawPreviewWindow = true;
                return;
            }

            switch (cell.Full)
            {
                case false: // If cell is empty, draw the preview in the cell
                    cell.UpdateEncounterHover(_playerChosenEnc);
                    _drawPreviewWindow = false;
                    if (TapTap.LeftClickInWorldView(cell.DestinationRect) && !TapTap.IsUIElementClicked())
                    {
                        ConfirmNewEncounter();
                    }
                    return;
                case true: // if cell is full, maintain the preview window draw
                    _drawPreviewWindow = true;
                    break;

            }

        }
        private static void ConfirmNewEncounter()
        {
            var chosenCell = _currentlyHoveredCell;
            var chosenEnc = _playerChosenEnc;

            chosenCell.AddEncounter(chosenEnc);
            List<ChargeData> chargesToAdd = chosenCell.FetchCharges();
            EncounterAdded?.Invoke(chargesToAdd);
            MarkFull(chosenCell);
            UIManager.ResetSpecificEncounter();
            UIManager.ResetEncounterCategory();
            TryToAddEmptyCells(chosenCell);
        }
        private static void TryToAddEmptyCells(Cell chosenCell)
        {
            int x = chosenCell.GridX;
            int y = chosenCell.GridY;

            // North, East, South, West offsets
            var directions = new (int dx, int dy)[]
            {
        (0, -1), // north
        (1, 0),  // east
        (0, 1),  // south
        (-1, 0), // west
            };

            foreach (var (dx, dy) in directions)
            {
                int nx = x + dx;
                int ny = y + dy;

                // Does a cell already exist at this grid coordinate?
                bool exists = _all.Any(c => c.GridX == nx && c.GridY == ny);
                if (exists)
                    continue;

                // Create a new empty cell (adjust this to your actual constructor / flags)
                var newCell = new Cell(nx, ny)
                {
                    // Example: mark this as empty if you have such a property
                    // IsEmpty = true
                };

                AddCell(newCell);
            }
        }
        public static void UpdatePlayerSelectedEncounter(string name)
        {
            _playerChosenEnc = name;
            _previewWindow.ReplaceAnimation(name);
        }

        private static bool _drawPreviewWindow = false;
        public static void DrawPreviewWindow(SpriteBatch s)
        {
            if (!_drawPreviewWindow) return;
            _previewWindow.DrawAnimatable(s);

        }
    }
}
