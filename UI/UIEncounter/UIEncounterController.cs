using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Data.SaveData;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.InputTracker;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;


namespace Heroes_UnWelcomed.UI.UIEncounter
{
    internal class UIEncounterController
    {
        private Dictionary<string, EncounterData> _unlockedEncData;
        private bool _showCategories = true;
        private EncounterType _selectedCategory = EncounterType.None;

        private EncounterCategoryButton EncCategoryButtons = new EncounterCategoryButton();
        private SpecificEncounterButtons _specificEncButtons = new SpecificEncounterButtons();
        public UIEncounterController()
        {
            UpdateUnlockedEncounters();
            UpdateUnlockedCategoryButtons();
            UpdateSpecificEncounterButtons();
            SaveStateLibrary.EncounterSaveStateUpdated += UpdateUnlockedEncounters;
            SaveStateLibrary.EncounterSaveStateUpdated += UpdateUnlockedCategoryButtons;
        }
        internal void Draw(SpriteBatch s)
        {
            EncCategoryButtons?.Draw(s);
            _specificEncButtons?.DrawButtons(s);
        }
        private void UpdateSpecificEncounterButtons()
        {
            _specificEncButtons.UpdateUnlockedEncounters(_unlockedEncData);
        }
        private void UpdateUnlockedCategoryButtons()
        {
            List<string> unlockedCategories = SaveStateLibrary.ReturnUnlockedEncounterCategories();
            EncCategoryButtons.RecreateButtons(unlockedCategories);

        }
        private void UpdateUnlockedEncounters()
        {
            _unlockedEncData = EncounterLibrary.ReturnUnlockedDictionary();

        }
        internal void Update(GameTime gameTime, UIInput input)
        {
            UpdateInput(input);

        }
        internal void UpdateInput(UIInput input)
        {
            EncCategoryButtons.UpdateButtons(input);
            _specificEncButtons.Update(_selectedCategory, input);
        }
    }
}
