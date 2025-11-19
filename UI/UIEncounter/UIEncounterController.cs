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

        private EncounterCategoryButton EncCategoryButtons = new EncounterCategoryButton();
        private SpecificEncounterButtons _specificEncButtons = new SpecificEncounterButtons();

        private EncounterData _selectedEncounter = null;
        public UIEncounterController()
        {
            UpdateUnlockedEncounters();
            UpdateUnlockedCategoryButtons();
            UpdateSpecificEncounterButtons();
            SaveStateLibrary.EncounterSaveStateUpdated += UpdateUnlockedEncounters;
            SaveStateLibrary.EncounterSaveStateUpdated += UpdateUnlockedCategoryButtons;
            SaveStateLibrary.EncounterSaveStateUpdated += UpdateSpecificEncounterButtons;

            EncCategoryButtons.OnCategoryChanged += UpdateSelectedEncounterCategory;
            _specificEncButtons.OnEncounterButtonSelected += UpdateCurrentlySelectedEncounter;
        }
        private void UpdateCurrentlySelectedEncounter(string name) 
        { 
            if (name == null)
            {
                _selectedEncounter = null;
                return;
            }
            _selectedEncounter = EncounterLibrary.GetEncounterData(name);
        }
        internal void Draw(SpriteBatch s)
        {
            EncCategoryButtons?.Draw(s);
            _specificEncButtons?.DrawButtons(s);
        }
        private void UpdateSpecificEncounterButtons()
        {
            Dictionary<EncounterType, Rectangle> catRectangles = EncCategoryButtons.GetCategoryRectangles();

            _specificEncButtons.UpdateUnlockedEncounters(_unlockedEncData, catRectangles);
        }
        private void UpdateUnlockedCategoryButtons()
        {
            List<EncounterType> unlockedCategories = SaveStateLibrary.ReturnUnlockedEncounterCategories();
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
            _specificEncButtons.Update(input);
        }
        internal void UpdateSelectedEncounterCategory(EncounterType category)
        {
            _specificEncButtons.UpdateCurrentEncOptions(category);
        }
    }
}
