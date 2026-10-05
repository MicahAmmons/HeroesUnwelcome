using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Rendering;

/// <summary>
/// Draws plain shapes and text by stretching a single white pixel. Every debug
/// box, outline and placeholder graphic is built from these.
/// </summary>
public sealed class Primitives : IDisposable
{
    private readonly Texture2D _pixel;

    public Primitives(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        Font = font;
    }

    public SpriteFont Font { get; }

    public void FillRect(SpriteBatch batch, Rectangle rect, Color color) =>
        batch.Draw(_pixel, rect, color);

    public void OutlineRect(SpriteBatch batch, Rectangle rect, Color color, int thickness = 2)
    {
        batch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        batch.Draw(_pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        batch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        batch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }

    public void Text(SpriteBatch batch, string text, Vector2 position, Color color, float scale = 1f) =>
        batch.DrawString(Font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

    /// <summary>Draws text centred inside a rectangle.</summary>
    public void TextCentered(SpriteBatch batch, string text, Rectangle area, Color color, float scale = 1f)
    {
        Vector2 size = Font.MeasureString(text) * scale;
        Vector2 position = new(area.Center.X - size.X / 2f, area.Center.Y - size.Y / 2f);
        Text(batch, text, new Vector2(MathF.Round(position.X), MathF.Round(position.Y)), color, scale);
    }

    public void Dispose() => _pixel.Dispose();
}
