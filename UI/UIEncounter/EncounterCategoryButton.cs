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
        private Dictionary<string, Button> _categoryButtons = new Dictionary<string, Button>();


        public EncounterCategoryButton()
        {
        }
        public void RecreateButtons(List<string> categories)
        {
            const int Margin = 50;
            CategoryButtons.Clear();

            int index = 0;
            foreach (var cat in categories)
            {
                Texture2D texture = AssetManager.GetTexture($"{cat}CategoryIcon");

                int width = texture.Width/2;
                int height = texture.Height/2;

                int x = Margin + index * (width + Margin);
                int y = ScreenSize.Height - Margin - height;

                CategoryButtons[cat] = new Button
                {
                    ButtonBoundary = new Rectangle(x, y, width, height),
                    Texture = texture
                };

                index++;
            }
        }

            // Figure out how I wanna communicate to this class that a button is chosen
            // and how to unchoose all others
            // then change how the surrounding class gives that info to the specific buttons
        
        public void Draw(SpriteBatch s)
        {
            if (_categoryButtons.Count == 0) return;

            foreach (var kvp in _categoryButtons)
            {
                kvp.Value.DrawButton(s);
            }
        }
        public void UpdateButtons(UIInput input)
        {
            foreach (var button in _categoryButtons.Values)
            {
                button.UpdateStatus(input.MousePos, input.LeftPressed);
            }
        }


    }
}
