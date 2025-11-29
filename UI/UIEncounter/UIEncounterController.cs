using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Cells;
using Heroes_UnWelcomed.Data.SaveData;
using Heroes_UnWelcomed.DebugBugger;
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

        public event Action UIButtonPressed;

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
        public void UpdateCurrentlySelectedEncounter(string name) 
        {
            Debug.UpdatePlayerSelectedEncounter(name);
            if (name == null)
            {
                if ( _selectedEncounter != null)
                {
                    _selectedEncounter = null;
                    CellManager.UpdatePlayerSelectedEncounter(null);
                }

                
                return;
            }
            _selectedEncounter = EncounterLibrary.GetEncounterData(name);
            UIButtonPressed?.Invoke();
            CellManager.UpdatePlayerSelectedEncounter(name);
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
            //Player clicks an encounter category
            UIButtonPressed?.Invoke();
            Debug.UpdateSelectedEncCategory(category);
            _specificEncButtons.UpdateCurrentEncOptions(category);
        }
        internal void ResetCurrentlySelectedSpecificEnc()
        {
            _specificEncButtons.UpdateCurrentlySelectedButton();
        }
    }
}
