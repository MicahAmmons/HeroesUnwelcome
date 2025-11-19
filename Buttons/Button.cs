using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Heroes_UnWelcomed.Buttons
{
    internal class Button
    {
        private bool _active;
        private bool _hovered;

        public Rectangle ButtonBoundary;
        public Texture2D Texture;

        public bool IsActive => _active;
        public bool IsHovered => _hovered;

        internal void UpdateStatus(Point mouse, bool leftJustReleased)
        {
            _hovered = ButtonBoundary.Contains(mouse);
            if (_hovered)
            {
                int th = 5;
            }
            if (_hovered && leftJustReleased)
            {
                _active = !_active;
            }
        }
        internal void DrawButton(SpriteBatch s)
        {
            var color = _active
                ? Color.Green
                : _hovered
                    ? Color.Yellow
                    : Color.White;

            s.Draw(
                texture: Texture,
                destinationRectangle: ButtonBoundary,
                sourceRectangle: null,
                color: color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
        internal void ResetButton()
        {
            _active = false;
        }
    }
}
