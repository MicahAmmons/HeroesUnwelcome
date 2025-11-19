using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Buttons;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.ScreenReso;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Security.AccessControl;

namespace Heroes_UnWelcomed.UI.UIEncounter
{
    internal class SpecificEncounterButtons
    {
        private Dictionary<EncounterType, Dictionary<string, Button>> _allButtons = new();
        private EncounterType _currentOptionType = EncounterType.None;

        public SpecificEncounterButtons()
        {

        }
        public void Update( UIInput input)
        {
            if (_currentOptionType == EncounterType.None) return;
            UpdateButtons(input);
        }
        public void DrawButtons(SpriteBatch s)
        {
            if (_currentOptionType == EncounterType.None)
                return;
            if (!_allButtons.TryGetValue(_currentOptionType, out var buttons))
                return;

            foreach (var button in buttons.Values)
                button.DrawButton(s);
        }
        public void UpdateUnlockedEncounters(Dictionary<string, EncounterData> unlockedEncData, Dictionary<EncounterType, Rectangle> catRectangles)
        {
            _allButtons.Clear();

            foreach (var kvp in unlockedEncData)
            {
                string encounterName = kvp.Key;
                EncounterData data = kvp.Value;
                EncounterType category = data.Category;

                if (!_allButtons.ContainsKey(category))
                    _allButtons[category] = new Dictionary<string, Button>();

                Texture2D tex = AssetManager.GetTexture($"{encounterName}Icon");

                Button btn = new Button
                {
                    ButtonBoundary = new Rectangle(0, 0, tex.Width, tex.Height),
                    Texture = tex
                };

                _allButtons[category][encounterName] = btn;
            }
            foreach (var category in _allButtons.Keys.ToList())
            {
                SetButtonRectangles(category, catRectangles[category]);
            }
        }
        private void SetButtonRectangles(EncounterType category, Rectangle anchorRect)
        {
            if (!_allButtons.TryGetValue(category, out var buttonsForType) ||
                buttonsForType.Count == 0)
                return;
            const int HorizontalMargin = 6;
            const int VerticalMargin = 10;
            int buttonWidth = anchorRect.Width / 2 - HorizontalMargin / 2;

            Button firstButton = buttonsForType.Values.FirstOrDefault(b => b.Texture != null);
            int buttonHeight;

            float aspect = firstButton.Texture.Height / (float)firstButton.Texture.Width;
            buttonHeight = (int)(buttonWidth * aspect);


            int index = 0;

            foreach (var kvp in buttonsForType.OrderBy(k => k.Key))
            {
                bool isLeftColumn = (index % 2 == 0);
                int row = index / 2; // 0 = first row above anchor, 1 = second row, etc.

                // X: left column at anchor.Left, right column to the right of it
                int x = isLeftColumn
                    ? anchorRect.Left
                    : anchorRect.Left + buttonWidth + HorizontalMargin;

                // Build upward:
                // First row bottom = anchor.Top - VerticalMargin
                // Each subsequent row: one buttonHeight + VerticalMargin higher
                int bottomY = anchorRect.Top
                              - VerticalMargin
                              - row * (buttonHeight + VerticalMargin);

                int y = bottomY - buttonHeight; // top-left for Rectangle

                kvp.Value.ButtonBoundary = new Rectangle(x, y, buttonWidth, buttonHeight);

                index++;
            }
        }
        public void UpdateButtons(UIInput input)
        {
            foreach (var kvp in _allButtons[_currentOptionType])
            {
                kvp.Value.UpdateStatus(input.MousePos, input.LeftPressed);
            }
        }
        public void UpdateCurrentEncOptions(EncounterType category)
        {
            _currentOptionType = category;
            ResetAllButtons();

        }
        private void ResetAllButtons()
        {
            foreach (var dict in _allButtons.Values)
                foreach (var button in dict.Values)
                    button.ResetButton();
        }


    }
}
