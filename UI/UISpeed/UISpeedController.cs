using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.UI.UISpeed
{
    internal class UISpeedController : ButtonManager
    {
        private Dictionary<string, Button> _allButtons = new Dictionary<string, Button>();
        public event Action<string> OnSpeedButtonClicked;
        public UISpeedController()
        {
            int x = 50;
            int y = 25;
            int xBuffer = 5;
            List<string> buttons = new List<string>()
            {
                "Play", "Pause", "2xSpeed", "3xSpeed"
            };
            for (int i = 0; i < buttons.Count; i++)
            {
                string name = buttons[i];

                _allButtons[buttons[i]] = new Button()
                {
                    Texture = AssetManager.GetTexture(name),
                    ButtonBoundary = new Rectangle(x * i, y, x, x),
                };
            }
        }

        public override void ButtonClicked(string key)
        {
            OnSpeedButtonClicked?.Invoke(key);
        }

        public override bool CheckIfButtonsAreActive()
        {
            return true;
        }

        public void Draw(SpriteBatch s)
        {
            if (_allButtons.Count == 0) return;
            foreach (var button in _allButtons.Values)
            {
                button.DrawButton(s);
            }
        }




    }
}
