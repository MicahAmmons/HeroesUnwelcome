using Heroes_UnWelcomed.UI;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Buttons
{
    public abstract class ButtonManager
    {
        public Dictionary<string, Button> _allCurrentButtons = new Dictionary<string, Button>();
        public Button _currentlySelectedButton;

        public virtual void UpdateButtons(UIInput input)
        {
            Button selectedButton = null;
            string key = null;

            foreach (var kvp in _allCurrentButtons)
            {
                Button btn = kvp.Value;
                btn.UpdateStatus(input.MousePos, input.LeftPressed);

                //This catches all buttons that are active
                if (btn.IsActive)
                {
                    if (btn != _currentlySelectedButton)
                    {
                        selectedButton = btn;
                        key = kvp.Key;
                        UpdateCurrentlySelectedButton(selectedButton, key);
                        ResetButtonsToInactive();
                    }
                    continue;
                }
                else if (btn == _currentlySelectedButton)
                {
                    ResetAllButtons();
                }

            }
        }
        public virtual void UpdateCurrentlySelectedButton(Button btn = null, string key = null)
        {
            if (_currentlySelectedButton == btn)
            {
                _currentlySelectedButton = null;
                ButtonClicked(key);

            }
            else
            {
                _currentlySelectedButton = btn;
                ButtonClicked(key);
            }
            ResetButtonsToInactive();
        }
        public abstract void ButtonClicked(string key);
        public virtual void Update(UIInput input)
        {
            CheckIfButtonsAreActive();

            UpdateButtons(input);
            ResetButtonsToInactive();
        }

        //Used to reset buttons without restting currenylselected and doesn't call ButtonClicked
        public virtual void ResetButtonsToInactive()
        {
            foreach (var kvp in _allCurrentButtons)
            {
                Button but = kvp.Value;
                if (_currentlySelectedButton == but) continue;

                but.ResetButton();
            }
        }

        // Use this to flat reset everything (such as exiting UI button menu)
        public virtual void ResetAllButtons(Button excludedButton = null)
        {
            foreach (var kvp in _allCurrentButtons)
            {
                Button but = kvp.Value;
                but.ResetButton();
            }
            _currentlySelectedButton = null;
            ButtonClicked(null);
        }
        public virtual void DrawButtons(SpriteBatch s)
        {
            if (!CheckIfButtonsAreActive()) return;

            foreach (var button in _allCurrentButtons.Values)
                button.DrawButton(s);
        }
        public abstract bool CheckIfButtonsAreActive();
    }
}
