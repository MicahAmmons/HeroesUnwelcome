using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Rendering;

/// <summary>
/// Draws plain shapes and text by stretching a single white pixel. Every debug
/// box, outline and placeholder graphic is built from these.
/// </summary>
public sealed class Primitives : IDisposable
{
    private const int RingTextureSize = 128;

    private readonly Texture2D _pixel;
    private readonly Texture2D _ring;
    private readonly Texture2D _disc;

    public Primitives(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _ring = CreateCircle(graphicsDevice, thickness: 0.14f);
        _disc = CreateCircle(graphicsDevice, thickness: 1f);
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

    /// <summary>An outlined ellipse (e.g. a portal) filling <paramref name="rect"/>.</summary>
    public void Ring(SpriteBatch batch, Rectangle rect, Color color) => batch.Draw(_ring, rect, color);

    /// <summary>A filled ellipse filling <paramref name="rect"/>.</summary>
    public void Disc(SpriteBatch batch, Rectangle rect, Color color) => batch.Draw(_disc, rect, color);

    // Draws a white circle into a texture once, with soft (anti-aliased) edges:
    // each pixel's opacity comes from how far it is from the circle's edge.
    private static Texture2D CreateCircle(GraphicsDevice device, float thickness)
    {
        const int size = RingTextureSize;
        var pixels = new Color[size * size];
        float radius = size / 2f - 1f;
        float inner = radius * (1f - thickness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f));
                float outerEdge = Math.Clamp(radius - distance + 0.5f, 0f, 1f);
                float innerEdge = thickness >= 1f ? 1f : Math.Clamp(distance - inner + 0.5f, 0f, 1f);
                pixels[y * size + x] = Color.White * (outerEdge * innerEdge); // premultiplied, like SpriteBatch expects
            }
        }

        var texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _ring.Dispose();
        _disc.Dispose();
    }
}
