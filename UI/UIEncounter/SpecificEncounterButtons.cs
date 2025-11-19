using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Buttons;
using Heroes_UnWelcomed.Encounters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
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
        public void Update(EncounterType currentSelection, UIInput input)
        {
            if (currentSelection == EncounterType.None) return;
            UpdateCurrentOptions(currentSelection);
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
        private void UpdateCurrentOptions(EncounterType currentSelection)
        {
            if (currentSelection == _currentOptionType)
                return;

            _currentOptionType = currentSelection;
        }
        public void UpdateUnlockedEncounters(Dictionary<string, EncounterData> unlockedEncData)
        {
            _allButtons.Clear();

            foreach (var kvp in unlockedEncData)
            {
                string encounterName = kvp.Key;
                EncounterData data = kvp.Value;
                EncounterType category = data.Category;

                if (!_allButtons.ContainsKey(category))
                    _allButtons[category] = new Dictionary<string, Button>();
                var text = AssetManager.GetTexture($"{encounterName}Icon");
                Button btn = new Button()
                {
                    ButtonBoundary = new Rectangle(0, 0, text.Width, text.Height), 
                    Texture = text
                };

                _allButtons[category][encounterName] = btn;
            }
        }
        public void UpdateButtons(UIInput input)
        {
            foreach (var kvp in _allButtons[_currentOptionType])
            {
                kvp.Value.UpdateStatus(input.MousePos, input.LeftPressed);
            }
        }
    }
}
