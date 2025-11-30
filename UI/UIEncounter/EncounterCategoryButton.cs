using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Buttons;
using Heroes_UnWelcomed.Data.SaveData;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.ScreenReso;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.UI.UIEncounter
{
    internal class EncounterCategoryButton 
    {
        private Dictionary<EncounterType, Button> _categoryButtons = new Dictionary<EncounterType, Button>();

        private EncounterType _selectedCategory = EncounterType.None;
        public event Action<EncounterType> OnCategoryChanged;


        public EncounterCategoryButton()
        {
        }
        public void RecreateButtons(List<EncounterType> categories)
        {
            const int Margin = 50;
            _categoryButtons.Clear();

            int index = 0;
            foreach (var cat in categories)
            {
                Texture2D texture = AssetManager.GetTexture($"{cat}CategoryIcon");

                int width = texture.Width/2;
                int height = texture.Height/2;

                int x = Margin + index * (width + Margin);
                int y = ScreenSize.Height - Margin - height;

                _categoryButtons[cat] = new Button
                {
                    ButtonBoundary = new Rectangle(x, y, width, height),
                    Texture = texture
                };

                index++;
            }
        }
        public void Draw(SpriteBatch s)
        {
            if (_categoryButtons.Count == 0) return;

            foreach (var kvp in _categoryButtons)
            {
                kvp.Value.DrawButton(s);
            }
        }
        public void ResetAllButtons()
        {
            foreach (var but in _categoryButtons.Values)
                but.ResetButton();
                    
        }
        public void UpdateButtons(UIInput input)
        {
            foreach (var kvp in _categoryButtons)
            {
                var button = kvp.Value;
                button.UpdateStatus(input.MousePos, input.LeftPressed);
            }
            var selected = _categoryButtons
              .Where(kvp => kvp.Value.IsActive)
              .Select(kvp => kvp.Key)
              .FirstOrDefault();

            if (selected != _selectedCategory)
            {
                _selectedCategory = selected;
                foreach (var kvp in _categoryButtons)
                {
                    if (kvp.Key != selected)
                        kvp.Value.ResetButton(); ;
                }
                OnCategoryChanged?.Invoke(_selectedCategory);
            }
        }
        internal Dictionary<EncounterType, Rectangle> GetCategoryRectangles()
        {
            Dictionary<EncounterType, Rectangle> dict = new Dictionary<EncounterType, Rectangle>();
            foreach (var kvp in _categoryButtons)
            {
                Button but = kvp.Value;
                EncounterType type = kvp.Key;
                dict[type] = but.ButtonBoundary;
                
            }
            return dict;
        }
    }
}
