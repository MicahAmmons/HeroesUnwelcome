using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.InputTracker;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.DebugBugger
{
    public static class Debug
    {
        private static string _currentHoveredCell = null;
        private static string _playerSelectedEncounter = null;
        private static string _playerSelectedEncCategory = null;
        private static bool _showing = false;
        private static SpriteFont _font;
        private static bool _uIButtonStartOfFrame;
        private static bool _uIButtonEndOfFrame;
        internal static void Initialize()
        {
            _font = AssetManager.GetFont("Debug");
        }
        public static void UpdateHoveredCell(int x = 1000, int y = 1000)
        {
            if (x == 1000 || y == 1000l)
            {
                _currentHoveredCell = $"No Hovered Cell";
                return;
                
            }
            _currentHoveredCell = $"Hovered Cell: {x}, {y}";
        }
        public static void UpdatePlayerSelectedEncounter(string data)
        {
            if (data == null)
            {
                _playerSelectedEncounter = "No selected EncounterData";
                return;
            }
            _playerSelectedEncounter = $"Currently Selected Enc: {data}";

        }
        public static void Update()
        {
            if ( TapTap.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.LeftControl))
            {
                _showing = !_showing;
            }
        }
        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!_showing)
                return;

            var viewport = spriteBatch.GraphicsDevice.Viewport;
            const int margin = 10;

            string hoveredText = _currentHoveredCell ?? "No Hovered Cell";
            string encounterText = _playerSelectedEncounter ?? "No selected EncounterData";
            string startFrameUI = $"Start Frame UI Pressed : {_uIButtonStartOfFrame}";
            string endFrameUI = $"End Frame UI Pressed : {_uIButtonEndOfFrame}";
            string encCatSelection = $"Selected Encounter Category: {_playerSelectedEncCategory}";
            if (_playerSelectedEncCategory == null)
            {
                encCatSelection = "No Selected Enc Category";
            }




            string[] lines =
            {
                hoveredText,
                encounterText,
                startFrameUI,
                endFrameUI,
                encCatSelection,
            };

            // Find max width for right-align
            float maxWidth = 0f;
            foreach (var line in lines)
            {
                var size = _font.MeasureString(line);
                if (size.X > maxWidth)
                    maxWidth = size.X;
            }

            float lineHeight = _font.LineSpacing;
            Vector2 topRightStart = new Vector2(
                viewport.Width - maxWidth - margin,
                margin
            );

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 pos = topRightStart + new Vector2(0, i * lineHeight);
                spriteBatch.DrawString(_font, lines[i], pos, Color.White);
            }
        }
        public static void UpdateStartOfFrameInput(bool isPressed)
        {
            _uIButtonStartOfFrame = isPressed;
        }
        public static void UpdateEndOfFrameInput(bool isPressed)
        {
            _uIButtonEndOfFrame = isPressed;
        }

        internal static void UpdateSelectedEncCategory(EncounterType type)
        {
            if (type == EncounterType.None)
            {
                _playerSelectedEncCategory = null;
                return;
            }
            _playerSelectedEncCategory = type.ToString();
        }
    }
}
